using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.OData.Query
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DefaultSkipTokenValueGenerator : ISkipTokenValueGenerator
    {
        private readonly char _commaDelimiter;
        private readonly char _propertyDelimiter;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commaDelimiter"></param>
        /// <param name="propertyDelimiter"></param>
        public DefaultSkipTokenValueGenerator(char commaDelimiter, char propertyDelimiter)
        {
            _commaDelimiter = commaDelimiter;
            _propertyDelimiter = propertyDelimiter;
        }

        /// <summary>
        /// Returns a function that converts an object to a skiptoken value string
        /// </summary>
        /// <param name="lastMember"> Object based on which SkipToken value will be generated.</param>
        /// <param name="model">The edm model.</param>
        /// <param name="orderByNodes">QueryOption </param>
        /// <returns></returns>
        public string GenerateSkipTokenValue(Object lastMember, IEdmModel model, IList<OrderByNode> orderByNodes)
        {
            object value;
            if (lastMember == null)
            {
                return String.Empty;
            }
            IEnumerable<IEdmProperty> propertiesForSkipToken = GetPropertiesForSkipToken(lastMember, model, orderByNodes);

            String skipTokenvalue = String.Empty;
            if (propertiesForSkipToken == null)
            {
                return skipTokenvalue;
            }

            int count = 0;
            int lastIndex = propertiesForSkipToken.Count() - 1;
            foreach (IEdmProperty property in propertiesForSkipToken)
            {
                bool islast = count == lastIndex;
                IEdmStructuredObject obj = lastMember as IEdmStructuredObject;
                if (obj != null)
                {
                    obj.TryGetPropertyValue(property.Name, out value);
                }
                else
                {
                    value = lastMember.GetType().GetProperty(property.Name).GetValue(lastMember);
                }

                String uriLiteral = String.Empty;
                if (value == null)
                {
                    uriLiteral = ODataUriUtils.ConvertToUriLiteral(value, ODataVersion.V401);
                }
                else if (TypeHelper.IsEnum(value.GetType()))
                {
                    ODataEnumValue enumValue = new ODataEnumValue(value.ToString(), value.GetType().FullName);
                    uriLiteral = ODataUriUtils.ConvertToUriLiteral(enumValue, ODataVersion.V401, model);
                    uriLiteral = "'enumType'" + uriLiteral;
                }
                else
                {
                    uriLiteral = ODataUriUtils.ConvertToUriLiteral(value, ODataVersion.V401, model);
                }
                skipTokenvalue += property.Name + _propertyDelimiter + uriLiteral + (islast ? String.Empty : _commaDelimiter.ToString());
                count++;
            }
            return skipTokenvalue;
        }

        private static IEnumerable<IEdmProperty> GetPropertiesForSkipToken(object lastMember, IEdmModel model, IList<OrderByNode> orderByNodes)
        {
            IEdmType edmType = GetTypeFromObject(lastMember, model);
            IEdmEntityType entity = edmType as IEdmEntityType;
            if (entity == null)
            {
                return null;
            }

            IList<IEdmProperty> key = entity.Key().AsIList<IEdmProperty>();
            if (orderByNodes != null)
            {
                OrderByOpenPropertyNode orderByOpenType = orderByNodes.OfType<OrderByOpenPropertyNode>().LastOrDefault();
                if (orderByOpenType != null)
                {
                    //SkipToken will not support ordering on dynamic properties
                    return null;
                }

                IList<IEdmProperty> orderByProps = orderByNodes.OfType<OrderByPropertyNode>().Select(p => p.Property).AsIList();
                foreach (IEdmProperty subKey in key)
                {
                    orderByProps.Add(subKey);
                }

                return orderByProps.AsEnumerable();
            }
            return key.AsEnumerable();
        }

        private static IEdmType GetTypeFromObject(object obj, IEdmModel model)
        {
            SelectExpandWrapper selectExpand = obj as SelectExpandWrapper;
            if (selectExpand != null)
            {
                IEdmTypeReference typeReference = selectExpand.GetEdmType();
                return typeReference.Definition;
            }

            Type clrType = obj.GetType();
            return EdmLibHelpers.GetEdmType(model, clrType);
        }
    }
}
