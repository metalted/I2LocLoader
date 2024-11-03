using System.Linq;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace I2LocLoader
{
    public static class I2LocLoaderClass
    {
        public static void SetLanguage(string language)
        {
            if (I2.Loc.LocalizationManager.HasLanguage(language))
            {
                I2.Loc.LocalizationManager.CurrentLanguage = language;

                Debug.Log("Set language to " + language);
            }
            else
            {
                Debug.Log("Language not found: " + language);
            }
        }

        public static List<string> GetCurrentLanguages()
        {
            List<string> languages = new List<string>();

            if (I2.Loc.LocalizationManager.Sources == null || I2.Loc.LocalizationManager.Sources.Count == 0)
            {
                Debug.Log("No language sources found in the system.");
                return languages;
            }

            // Loop through each LanguageSource in LocalizationManager
            foreach (I2.Loc.LanguageSourceData source in I2.Loc.LocalizationManager.Sources)
            {
                foreach (I2.Loc.LanguageData language in source.mLanguages)
                {
                    languages.Add(language.Name);
                }
            }

            return languages;
        }

        public static string GetEnglishTermsCSV()
        {
            if (I2.Loc.LocalizationManager.Sources == null || I2.Loc.LocalizationManager.Sources.Count == 0)
            {
                Debug.LogWarning("No LocalizationManager sources found.");
                return string.Empty;
            }

            I2.Loc.LanguageSourceData sourceData = I2.Loc.LocalizationManager.Sources[0];

            // Find the index of English language
            int englishIndex = sourceData.GetLanguageIndex("English");
            if (englishIndex < 0)
            {
                Debug.LogWarning("English language not found in the LanguageSource.");
                return string.Empty;
            }

            // Prepare StringBuilder for CSV content
            StringBuilder csvContent = new StringBuilder();

            csvContent.AppendLine("Key,English");

            // Iterate through all terms in the source
            foreach (var termData in sourceData.mTerms)
            {
                if(termData.Term.Contains("Demo"))
                {
                    continue;
                }

                // Add term key
                csvContent.Append(termData.Term);

                // Retrieve the English translation if available
                string translation = termData.Languages != null && englishIndex < termData.Languages.Length
                    ? termData.Languages[englishIndex]
                    : string.Empty; // Ensure no out-of-bounds access

                // Escape commas or quotes in translation to preserve CSV format
                translation = translation.Contains(",") || translation.Contains("\"")
                    ? $"\"{translation.Replace("\"", "\"\"")}\""
                    : translation;

                // Add the English translation
                csvContent.Append($",{translation}");
                csvContent.AppendLine();
            }

            // Return the CSV content as a string
            return csvContent.ToString();
        }

        public static void LoadLanguageCSV(string path)
        {
            if(I2.Loc.LocalizationManager.Sources == null || I2.Loc.LocalizationManager.Sources.Count == 0)
            {
                Debug.LogWarning("LocalizationManager not found or no sources found in LocalizationManager.");
                return;
            }

            I2.Loc.LanguageSourceData languageSourceData = I2.Loc.LocalizationManager.Sources[0];

            if (!File.Exists(path))
            {
                Debug.LogWarning($"CSV file not found: {path}");
                return;
            }

            string[] csvLines = File.ReadAllLines(path);
            if (csvLines.Length == 0)
            {
                Debug.LogWarning("CSV file is empty.");
                return;
            }

            string[] headers = csvLines[0].Split(',');
            if (headers.Length != 2)
            {
                Debug.LogWarning("Incorrect amount of columns. Expected: 2. Found: " + headers.Length + ".");
            }

            if (headers[0].ToLower() != "key")
            {
                Debug.LogWarning("Incorrect header name. Expected: Key/key. Found:" + headers[0] + ".");
                return;
            }

            string language = headers[1].Trim();
            if (string.IsNullOrWhiteSpace(language))
            {
                Debug.LogWarning("Language header is empty.");
                return;
            }

            foreach (string line in csvLines.Skip(1))
            {
                string[] fields = line.Split(',');
                if (fields.Length == 0)
                {
                    Debug.LogWarning("Skipping empty line in CSV.");
                    continue;
                }

                if (fields.Length != 2)
                {
                    Debug.LogWarning("Incorrect amount of fields. (" + line + ")");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(fields[0].Trim()))
                {
                    Debug.LogWarning("Key cannot be empty.");
                    continue;
                }

                ImportTerm(fields[0], language, fields[1]);
            }

            languageSourceData.UpdateDictionary();

            Debug.Log("Succesfully imported language: " + language);
        }
        private static void ImportTerm(string termKey, string language, string translation)
        {
            I2.Loc.LanguageSourceData languageSourceData = I2.Loc.LocalizationManager.Sources[0];

            // Find or add the language
            int langIndex = languageSourceData.GetLanguageIndex(language, false, false);
            if (langIndex < 0)
            {
                languageSourceData.AddLanguage(language, I2.Loc.GoogleLanguages.GetLanguageCode(language));
                langIndex = languageSourceData.GetLanguageIndex(language, false, false);
            }

            I2.Loc.TermData termData = languageSourceData.GetTermData(termKey);
            if (termData == null)
            {
                termData = languageSourceData.AddTerm(termKey, I2.Loc.eTermType.Text);
            }

            termData.Languages[langIndex] = translation;
        }
    }
}
