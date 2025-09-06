using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationLib.Interface
{
    public interface IOperateResult<T>
    {
        bool IsSuccess();

        string GetMessage();

        int GetErrorCode();

        T Value();

    }
}
