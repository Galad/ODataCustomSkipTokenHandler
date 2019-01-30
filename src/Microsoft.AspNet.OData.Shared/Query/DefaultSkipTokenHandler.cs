// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.AspNet.OData.Common;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.AspNet.OData.Query
{
    /// <summary>
    /// Default implementation of SkipTokenHandler for the service. 
    /// </summary>
    public class DefaultSkipTokenHandler : SkipTokenHandler
    {
        private IDictionary<string, object> _propertyValuePairs;
        private const char CommaDelimiter = ',';
        private readonly ISkipTokenValueGenerator _skipTokenValueGenerator;
        private string _value;
        private readonly char _propertyDelimiter;

        /// <summary>
        /// Constructor for Unit testing purposes - Sets the Property Delimiter
        /// </summary>
        public DefaultSkipTokenHandler() : this(':')
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delimiter"></param>
        public DefaultSkipTokenHandler(char delimiter) : this(delimiter, new DefaultSkipTokenValueGenerator(CommaDelimiter, delimiter))
        {
        }

        /// <summary>
        /// Constructor for DefaultSkipTokenHandler - Sets the Property Delimiter
        /// </summary>
        public DefaultSkipTokenHandler(char delimiter, ISkipTokenValueGenerator skipTokenValueGenerator)
        {
            _propertyDelimiter = delimiter;
            IsDeltaFeedSupported = false;
            this._skipTokenValueGenerator = skipTokenValueGenerator;
        }

        /// <summary>
        /// Process SkipToken Value to create string key - object value collection 
        /// </summary>
        public override string Value
        {
            get
            {
                return _value;
            }
            set
            {
                Contract.Assert(Context != null);

                _value = value;
                _propertyValuePairs = new Dictionary<string, object>();
                string[] keyValues = _value.Split(CommaDelimiter);
                foreach (string keyAndValue in keyValues)
                {
                    string[] pieces = keyAndValue.Split(new char[] { _propertyDelimiter }, 2);
                    if (pieces.Length > 1 && !String.IsNullOrWhiteSpace(pieces[1]))
                    {
                        object propValue = null;
                        if (pieces[1].StartsWith("'enumType'"))
                        {
                            string enumValue = pieces[1].Remove(0, 10);
                            IEdmTypeReference type = EdmLibHelpers.GetTypeReferenceOfProperty(Context.Model, Context.ElementClrType, pieces[0]);
                            propValue = ODataUriUtils.ConvertFromUriLiteral(enumValue, ODataVersion.V401, Context.Model, type);
                        }
                        else
                        {
                            propValue = ODataUriUtils.ConvertFromUriLiteral(pieces[1], ODataVersion.V401);
                        }
                        if (!String.IsNullOrWhiteSpace(pieces[0]))
                        {
                            _propertyValuePairs.Add(pieces[0], propValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the URI for NextPageLink
        /// </summary>
        /// <param name="baseUri">BaseUri for nextlink. It should be request URI for top level resource and navigationlink for nested resource.</param>
        /// <param name="instance">Instance based on which SkipToken value will be generated.</param>
        /// <param name="pageSize">Maximum number of records in the set of partial results for a resource.</param>
        /// <param name="context">Serializer context</param>
        /// <returns></returns>
        public override Uri GenerateNextPageLink(Uri baseUri, int pageSize, Object instance, ODataSerializerContext context)
        {
            if (context == null || instance == null)
            {
                return null;
            }

            Func<object, string> skipTokenGenerator = null;
            IList<OrderByNode> orderByNodes = null;
            ExpandedNavigationSelectItem expandedItem = context.ExpandedNavigationSelectItem;
            IEdmModel model = context.Model;

            if (expandedItem != null)
            {
                if (expandedItem.OrderByOption != null)
                {
                    orderByNodes = OrderByNode.CreateCollection(expandedItem.OrderByOption);
                }

                skipTokenGenerator = (obj) =>
                {
                    return _skipTokenValueGenerator.GenerateSkipTokenValue(obj, model, orderByNodes);
                };

                return GetNextPageHelper.GetNextPageLink(baseUri, pageSize, instance, skipTokenGenerator);
            }

            if (context.QueryOptions != null && context.QueryOptions.OrderBy != null)
            {
                orderByNodes = context.QueryOptions.OrderBy.OrderByNodes;
            }

            skipTokenGenerator = (obj) =>
            {
                return _skipTokenValueGenerator.GenerateSkipTokenValue(obj, model, orderByNodes);
            };

            return context.InternalRequest.GetNextPageLink(pageSize, instance, skipTokenGenerator);
        }

        /// <summary>
        /// Delimiter used to separate property and value, making it a public property for the purpose of testing
        /// </summary>
        public char PropertyDelimiter { get; set; }

        /// <summary>
        /// Apply the $skiptoken query to the given IQueryable.
        /// </summary>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="skipTokenQueryOption">The skiptoken query option which needs to be applied to this query option.</param>
        /// <returns>The new <see cref="IQueryable"/> after the skiptoken query has been applied to.</returns>
        public override IQueryable<T> ApplyTo<T>(IQueryable<T> query, SkipTokenQueryOption skipTokenQueryOption)
        {
            if (skipTokenQueryOption == null)
            {
                throw Error.ArgumentNullOrEmpty("skipTokenQueryOption");
            }

            ODataQuerySettings querySettings = skipTokenQueryOption.QuerySettings;
            IList<OrderByNode> orderByNodes = skipTokenQueryOption.OrderByNodes;
            return ApplyToCore(query, querySettings, orderByNodes) as IOrderedQueryable<T>;
        }

        /// <summary>
        /// Apply the $skiptoken query to the given IQueryable.
        /// </summary>
        /// <param name="query">The original <see cref="IQueryable"/>.</param>
        /// <param name="skipTokenQueryOption">The skiptoken query option which needs to be applied to this query option.</param>
        /// <returns>The new <see cref="IQueryable"/> after the skiptoken query has been applied to.</returns>
        public override IQueryable ApplyTo(IQueryable query, SkipTokenQueryOption skipTokenQueryOption)
        {
            if (skipTokenQueryOption == null)
            {
                throw Error.ArgumentNullOrEmpty("skipTokenQueryOption");
            }

            ODataQuerySettings querySettings = skipTokenQueryOption.QuerySettings;
            IList<OrderByNode> orderByNodes = skipTokenQueryOption.OrderByNodes;
            return ApplyToCore(query, querySettings, orderByNodes);
        }

        private IQueryable ApplyToCore(IQueryable query, ODataQuerySettings querySettings, IList<OrderByNode> orderByNodes)
        {
            if (Context.ElementClrType == null)
            {
                throw Error.NotSupported(SRResources.ApplyToOnUntypedQueryOption, "ApplyTo");
            }
            ExpressionBinderBase binder = new FilterBinder(Context.RequestContainer);
            IDictionary<string, OrderByDirection> directionMap = PopulateDirections(orderByNodes);
            bool parameterizeConstant = querySettings.EnableConstantParameterization;
            ParameterExpression param = Expression.Parameter(Context.ElementClrType);
            Expression where = null;
            /* We will create a where lambda of the following form -
             * Where (Prop1>Value1)
             * OR (Prop1=Value1 AND Prop2>Value2)
             * OR (Prop1=Value1 AND Prop2=Value2 AND Prop3>Value3)
             * and so on...
             * Adding the first true to simplify implementation.
             */
            Expression lastEquality = null;
            bool firstProperty = true;
            foreach (KeyValuePair<string, object> item in _propertyValuePairs)
            {
                string key = item.Key;
                MemberExpression property = Expression.Property(param, key);
                object value = item.Value;

                Expression compare = null;
                ODataEnumValue enumValue = value as ODataEnumValue;
                if (enumValue != null)
                {
                    value = enumValue.Value;
                }
                Expression constant = parameterizeConstant ? LinqParameterContainer.Parameterize(value.GetType(), value) : Expression.Constant(value);
                if (directionMap.ContainsKey(key))
                {
                    compare = directionMap[key] == OrderByDirection.Descending ? binder.CreateBinaryExpression(BinaryOperatorKind.LessThan, property, constant, true) : binder.CreateBinaryExpression(BinaryOperatorKind.GreaterThan, property, constant, true);
                }
                else
                {
                    compare = binder.CreateBinaryExpression(BinaryOperatorKind.GreaterThan, property, constant, true);
                }

                if (firstProperty)
                {
                    lastEquality = binder.CreateBinaryExpression(BinaryOperatorKind.Equal, property, constant, true);
                    where = compare;
                    firstProperty = false;
                }
                else
                {
                    Expression condition = Expression.AndAlso(lastEquality, compare);
                    where = where == null ? condition : Expression.OrElse(where, condition);
                    lastEquality = Expression.AndAlso(lastEquality, binder.CreateBinaryExpression(BinaryOperatorKind.Equal, property, constant, true));
                }
            }

            Expression whereLambda = Expression.Lambda(where, param);
            return ExpressionHelpers.Where(query, whereLambda, query.ElementType);
        }

        private static IDictionary<string, OrderByDirection> PopulateDirections(IList<OrderByNode> orderByNodes)
        {
            IDictionary<string, OrderByDirection> directions = new Dictionary<string, OrderByDirection>();
            if (orderByNodes == null)
            {
                return directions;
            }

            foreach (OrderByPropertyNode node in orderByNodes)
            {
                if (node != null)
                {
                    directions[node.Property.Name] = node.Direction;
                }
            }
            return directions;
        }
    }
}
