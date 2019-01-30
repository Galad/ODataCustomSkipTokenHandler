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
        private class HiddenIdSkipTokenValueGenerator : ISkipTokenValueGenerator
        {
            private readonly DefaultSkipTokenValueGenerator _defaultSkipTokenValueGenerator;

            public HiddenIdSkipTokenValueGenerator()
            {
                _defaultSkipTokenValueGenerator = new DefaultSkipTokenValueGenerator(',', ':');
            }

            public string GenerateSkipTokenValue(object lastMember, IEdmModel model, IList<OrderByNode> orderByNodes)
            {
                if(lastMember is Record record)
                {
                    return record.HiddenId.ToString();
                }
                return _defaultSkipTokenValueGenerator.GenerateSkipTokenValue(lastMember, model, orderByNodes);
            }
        }

        public HiddenIdSkipTokenHandler():base(':', new HiddenIdSkipTokenValueGenerator())
        {
        }

        public override IQueryable<T> ApplyTo<T>(IQueryable<T> query, SkipTokenQueryOption skipTokenQueryOption)
        {
            if (typeof(Movie) == typeof(T))
            {
                return (IQueryable<T>)ApplyToInternal((IQueryable<Movie>)query, skipTokenQueryOption);
            }
            return base.ApplyTo(query, skipTokenQueryOption);
        }
        
        private IQueryable<T> ApplyToInternal<T>(IQueryable<T> query, SkipTokenQueryOption skipTokenQueryOption)
            where T : Record
        {
            return query.Where(r => r.HiddenId > _skipTokenValue);
        }

        public override IQueryable ApplyTo(IQueryable query, SkipTokenQueryOption skipTokenQueryOption)
        {
            if(query is IQueryable<Movie> movieQueryable)
            {
                return ApplyToInternal(movieQueryable, skipTokenQueryOption);
            }
            return base.ApplyTo(query, skipTokenQueryOption);
        }
               
        private int? _skipTokenValue;
        public override string Value
        {
            get
            {
                if (!_skipTokenValue.HasValue)
                {
                    return base.Value;
                }
                return _skipTokenValue.Value.ToString();
            }
            set
            {
                if(int.TryParse(value, out var skipTokenValue))
                {
                    _skipTokenValue = skipTokenValue;
                }
                else
                {
                    base.Value = value;
                }
            }
        }
    }
}