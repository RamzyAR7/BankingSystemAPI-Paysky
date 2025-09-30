using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using BankingSystemAPI.Application.Common;

namespace BankingSystemAPI.Application.Interfaces.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }

    public interface IQuery : IRequest<Result>
    {
    }
}
