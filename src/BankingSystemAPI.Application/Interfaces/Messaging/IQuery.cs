#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using BankingSystemAPI.Domain.Common;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    }

    public interface IQuery : IRequest<Result>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
    }
}

