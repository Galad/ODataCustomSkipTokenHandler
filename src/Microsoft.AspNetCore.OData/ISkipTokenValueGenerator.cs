using Microsoft.OData.Edm;
using System.Collections.Generic;

namespace Microsoft.AspNet.OData.Query
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISkipTokenValueGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastMember"></param>
        /// <param name="model"></param>
        /// <param name="orderByNodes"></param>
        /// <returns></returns>
        string GenerateSkipTokenValue(object lastMember, IEdmModel model, IList<OrderByNode> orderByNodes);
    }
}