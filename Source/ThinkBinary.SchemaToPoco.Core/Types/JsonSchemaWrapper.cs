﻿using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkBinary.SchemaToPoco.Core.Types
{
    /// <summary>
    /// Wrapper for a JsonSchema.
    /// </summary>
    public class JsonSchemaWrapper
    {
        /// <summary>
        /// Set defaults for required fields.
        /// </summary>
        public const string DefaultClassName = "DefaultClassName";

        /// <summary>
        /// The JsonSchema.
        /// </summary>
        public JsonSchema Schema { get; set; }
        
        /// <summary>
        /// Namespace for this JSON schema to use.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Whether or not this schema should be generated or just referenced.
        /// </summary>
        public bool ToCreate { get; set; }

        /// <summary>
        /// List of interfaces.
        /// </summary>
        public List<Type> Interfaces { get; set; }

        public JsonSchemaWrapper(JsonSchema schema)
        {
            Schema = schema;

            // Initialize defaults
            ToCreate = true;
            Interfaces = new List<Type>();

            Schema.Title = Schema.Title ?? DefaultClassName;
        }
    }
}
