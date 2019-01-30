// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCoreODataSample.Web.Models;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData.Edm;

namespace AspNetCoreODataSample.Web
{
    public class HiddenIdSkipTokenHandler : DefaultSkipTokenHandler
    {
        private int? _skipTokenValue;
        
        public override IQueryable<T> ApplyTo<T>(IQueryable<T> query, ODataQuerySettings querySettings, IList<OrderByNode> orderByNodes)
        {
            if (typeof(Movie) == typeof(T))
            {
                return (IQueryable<T>)ApplyToInternal((IQueryable<Movie>)query, querySettings);
            }
            return base.ApplyTo(query, querySettings, orderByNodes);
        }

        private IQueryable<T> ApplyToInternal<T>(IQueryable<T> query, ODataQuerySettings querySettings)
            where T : Record
        {
            return query.Where(r => r.HiddenId > _skipTokenValue);
        }

        public override IQueryable ApplyTo(IQueryable query, ODataQuerySettings querySettings, IList<OrderByNode> orderByNodes)
        {
            if(query is IQueryable<Movie> movieQueryable)
            {
                return ApplyToInternal(movieQueryable, querySettings);
            }
            return base.ApplyTo(query, querySettings, orderByNodes);
        }
        
        public override string GenerateSkipTokenValue(object lastMember, IEdmModel model, IList<OrderByNode> orderByNodes)
        {
            if (lastMember is Record record)
            {
                return record.HiddenId.ToString();
            }
            return base.GenerateSkipTokenValue(lastMember, model, orderByNodes);
        }

        public override IDictionary<string, object> ProcessSkipTokenValue(string rawValue)
        {
            if (!int.TryParse(rawValue, out var skipTokenValue))
            {
                return base.ProcessSkipTokenValue(rawValue);
            }
            _skipTokenValue = skipTokenValue;
            return new Dictionary<string, object>() { { nameof(Record.HiddenId), skipTokenValue } };
        }
    }
}