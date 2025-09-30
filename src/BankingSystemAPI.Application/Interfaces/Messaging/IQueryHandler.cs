using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using BankingSystemAPI.Application.Common;

namespace BankingSystemAPI.Application.Interfaces.Messaging
{
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
    {
    }

    public interface IQueryHandler<TQuery> : IRequestHandler<TQuery, Result> where TQuery : IQuery
    {
    }
}
