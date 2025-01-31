using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public abstract class FieldDefinitionBase
        : DefinitionBase
        , IHasDirectiveDefinition
    {
        private List<DirectiveDefinition>? _directives;

        /// <summary>
        /// Gets the field type.
        /// </summary>
        public ITypeReference? Type { get; set; }

        /// <summary>
        /// Defines if this field is ignored and will
        /// not be included into the schema.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Gets the list of directives that are annotated to this field.
        /// </summary>
        public IList<DirectiveDefinition> Directives =>
            _directives ??= new List<DirectiveDefinition>();

        /// <summary>
        /// Gets the list of directives that are annotated to this field.
        /// </summary>
        public IReadOnlyList<DirectiveDefinition> GetDirectives()
        {
            if (_directives is null)
            {
                return Array.Empty<DirectiveDefinition>();
            }

            return _directives;
        }

        protected void CopyTo(FieldDefinitionBase target)
        {
            base.CopyTo(target);

            if (_directives is not null)
            {
                target._directives = new List<DirectiveDefinition>(_directives);
            }

            target.Type = Type;
            target.Ignore = Ignore;
        }
    }
}
