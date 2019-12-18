﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using Cvent.SchemaToPoco.Core.CodeToLanguage;
using Cvent.SchemaToPoco.Core.Types;
using Cvent.SchemaToPoco.Core.Util;
using Cvent.SchemaToPoco.Core.Wrappers;
using Cvent.SchemaToPoco.Types;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;

namespace Cvent.SchemaToPoco.Core
{
    /// <summary>
    ///     Main controller.
    /// </summary>
    public class JsonSchemaToPoco
    {
        /// <summary>
        ///     Logger.
        /// </summary>
        private Logger _log;


        /// <summary>
        ///     Configuration.
        /// </summary>
        private readonly JsonSchemaToPocoConfiguration _configuration;

        /// <summary>
        ///     Keeps track of the found schemas.
        /// </summary>
        private Dictionary<Uri, JsonSchemaWrapper> _schemas = new Dictionary<Uri, JsonSchemaWrapper>();

        /// <summary>
        ///     Initialize settings.
        /// </summary>
        public JsonSchemaToPoco(JsonSchemaToPocoConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///     Main executor method.
        /// </summary>
        /// <returns>A status code.</returns>
        public int Execute()
        {
            try
            {
                ConfigureLogging();

                // Load schemas given a json file or directory
                LoadSchemas();

                // Generate code
                Generate();

                return (int)ExitCodes.Ok;
            }
            catch (Exception e)
            {
                _log.Fatal(e);
                return (int)ExitCodes.AbnormalExit;
            }
        }

        /// <summary>
        ///     Configuring the logger.
        /// </summary>
        private void ConfigureLogging()
        {
            var coloredConsoleTarget = new ColoredConsoleTarget
            {
                Layout = "${time:format=hh:mm:ss} [${level}] ${message}"
            };
            var loggingRule = new LoggingRule("*", LogLevel.Debug, coloredConsoleTarget);
            LogManager.Configuration = new LoggingConfiguration();
            LogManager.Configuration.AddTarget("Console", coloredConsoleTarget);
            LogManager.Configuration.LoggingRules.Add(loggingRule);
            LogManager.ReconfigExistingLoggers();

            _log = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        ///     Load all the schemas from a file (or directory).
        /// </summary>
        private void LoadSchemas()
        {
            var resolver = new JsonSchemaResolver(_configuration.Namespace, !_configuration.Verbose, _configuration.OutputDirectory);
            FileAttributes attr = File.GetAttributes(_configuration.JsonSchemaFileLocation);

            // if specified path is a directory, load schemas from each file, otherwise just load schemas from that path
            bool isDirectory = (attr & FileAttributes.Directory) > 0;
            if (isDirectory)
            {
                _schemas = new Dictionary<Uri, JsonSchemaWrapper>();
                foreach (String fileName in Directory.GetFiles(_configuration.JsonSchemaFileLocation))
                {
                    Dictionary<Uri, JsonSchemaWrapper> resolvedSchemas = resolver.ResolveSchemas(fileName);
                    foreach (Uri key in resolvedSchemas.Keys)
                    {
                        // Schemas may refer to eachother, don't add the same schema twice just because it was included by reference
                        if (!_schemas.ContainsKey(key))
                        {
                            _schemas.Add(key, resolvedSchemas[key]);
                        }
                    }
                }
            }
            else
            {
                _schemas = resolver.ResolveSchemas(_configuration.JsonSchemaFileLocation);
            }
        }

        /// <summary>
        ///     Generate C# code.
        /// </summary>
        private void Generate()
        {
            var generatedCode = GenerateHelper();
            var fileExtension = GetFileExtension();

            // Create directory to generate files
            if (!_configuration.Verbose)
            {
                IoUtils.CreateDirectoryFromNamespace(_configuration.OutputDirectory, _configuration.Namespace);
            }

            string entryNamespace = String.Empty;
            Dictionary<string,string> moduleList = new Dictionary<string, string>();

            foreach (var entry in generatedCode)
            {
                if (!_configuration.Verbose)
                {
                    string saveLoc;
                    if (_configuration.LanguageExportType == LanguageExportType.Php || _configuration.LanguageExportType == LanguageExportType.Python)
                    {
                        entryNamespace = entry.Key.Namespace;
                        saveLoc = Path.Combine(_configuration.OutputDirectory, entry.Key.Namespace.Replace('.', Path.DirectorySeparatorChar), StringUtils.LowerFirst(entry.Key.Schema.Title) + fileExtension);

                        if (!moduleList.ContainsKey("\"" + StringUtils.LowerFirst(entry.Key.Schema.Title) + "\""))
                            moduleList.Add("\"" + StringUtils.LowerFirst(entry.Key.Schema.Title) + "\"", "1");

                        IoUtils.GenerateFile(StringUtils.LowerFirst(entry.Value), saveLoc);
                    }
                    else
                    {
                        saveLoc = Path.Combine(_configuration.OutputDirectory, entry.Key.Namespace.Replace('.', Path.DirectorySeparatorChar), entry.Key.Schema.Title + fileExtension);
                        IoUtils.GenerateFile(entry.Value, saveLoc);
                    }
                    Console.WriteLine("Wrote " + saveLoc);
                }
                else
                {
                    Console.WriteLine(entry.Value);
                }
            }

            if (_configuration.LanguageExportType == LanguageExportType.Python)
            {
                CreateInitPyFiles(entryNamespace, moduleList);
            }

        }

        private void CreateInitPyFiles(string entryNamespace, Dictionary<string, string> moduleList)
        {
            var initpyFile = Path.Combine(_configuration.OutputDirectory, entryNamespace.Replace('.', Path.DirectorySeparatorChar), "__init__.py");
            Console.WriteLine("Wrote " + initpyFile);
            var directories = entryNamespace.Split(".".ToCharArray());

            string allVar = "__all__ = [";
            var sep = "";
            foreach (var module in moduleList)
            {
                allVar += sep + module.Key;
                sep = ",\n" + new String(' ', 11);
            }
            IoUtils.GenerateFile(allVar + "]", initpyFile);

            //ignore last directory, we just created that file
            //before this code block.
            var dirName = "";
            for (int i = 0; i < directories.Length - 1; i++)
            {
                dirName = Path.Combine(dirName, directories[i]);
                var initpyEmptyFile = Path.Combine(_configuration.OutputDirectory, dirName, "__init__.py");                
                IoUtils.GenerateFile(string.Empty, initpyEmptyFile);
                Console.WriteLine("Wrote " + initpyEmptyFile);
            }
        }

        private string GetFileExtension()
        {
            return _configuration.LanguageExportType.GetDescription();
        }

        /// <summary>
        ///     Return a Dictionary containing a map of the generated JsonSchemaWrappers with the generated code as a string.
        /// </summary>
        /// <returns>A mapping of all the JSON schemas and the generated code.</returns>
        private Dictionary<JsonSchemaWrapper, string> GenerateHelper()
        {
            var generatedCode = new Dictionary<JsonSchemaWrapper, string>();

            foreach (JsonSchemaWrapper s in _schemas.Values)
            {
                if (s.ToCreate)
                {
                    var jsonSchemaToCodeUnit = new JsonSchemaToCodeUnit(s, s.Namespace, _configuration.AttributeType);
                    CodeCompileUnit codeUnit = jsonSchemaToCodeUnit.Execute();
                    var generator = GetCodeCompileUnit(codeUnit);
                    generatedCode.Add(s, generator.Execute());
                }
            }

            return generatedCode;
        }

        private CodeCompileUnitToLanguageBase GetCodeCompileUnit(CodeCompileUnit codeUnit)
        {
            CodeCompileUnitToLanguageBase returnValue;
            switch (_configuration.LanguageExportType)
            {
                case LanguageExportType.Php:
                    returnValue = new CodeCompileUnitToPHP(codeUnit);
                    break;
                case LanguageExportType.CSharp:
                    returnValue = new CodeCompileUnitToCSharp(codeUnit);
                    break;
                case LanguageExportType.Python:
                    returnValue = new CodeCompileUnitToPython(codeUnit);
                    break;
                default:
                    throw new Exception("Unknown language export"); 
            }
            return returnValue;
        }

        /// <summary>
        ///     Static method to return a Dictionary of JsonSchemaWrapper and its corresponding C# generated code.
        /// </summary>
        /// <param name="schemaLoc">Location of JSON schema.</param>
        /// <returns>A mapping of all the JSON schemas and the generated code.</returns>
        public static Dictionary<JsonSchemaWrapper, string> GenerateFromFile(string schemaLoc)
        {
            var controller = new JsonSchemaToPoco(
                new JsonSchemaToPocoConfiguration
                {
                    JsonSchemaFileLocation = schemaLoc
                }
            );
            controller.LoadSchemas();
            return controller.GenerateHelper();
        }

        /// <summary>
        ///     Static method to return generated code for a single JSON schema with no references.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <returns>The generated code.</returns>
        public static string Generate(JsonSchemaToPocoConfiguration configuration)
        {
            return Generate(configuration.JsonSchemaFileLocation, configuration.Namespace, configuration.AttributeType);
        }

        /// <summary>
        ///     Static method to return generated code for a single JSON schema with no references.
        /// </summary>
        /// <param name="schema">Location of schema file.</param>
        /// <param name="ns">The namespace.</param>
        /// <param name="type">The attribute type.</param>
        /// <returns>The generated code.</returns>
        public static string Generate(string schema, string ns = "generated", AttributeType type = AttributeType.SystemDefault)
        {
            var jsonSchemaToCodeUnit = new JsonSchemaToCodeUnit(JsonSchemaResolver.ConvertToWrapper(schema), ns, type);
            CodeCompileUnit codeUnit = jsonSchemaToCodeUnit.Execute();
            var csharpGenerator = new CodeCompileUnitToCSharp(codeUnit);
            return csharpGenerator.Execute();
        }
    }
}
