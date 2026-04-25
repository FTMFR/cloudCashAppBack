using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.DTOs.Common
{
    public class ResultDto
    {
        public ResultDto()
        {

        }
        public ResultDto(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
        public ResultDto(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
    public class ResultDto<T> : ResultDto
    {
        public ResultDto()
        {

        }
        public ResultDto(bool isSuccess) : base(isSuccess)
        {

        }
        public ResultDto(bool isSuccess, T data) : this(isSuccess, null, data)
        {

        }
        public ResultDto(bool isSuccess, string message) : base(isSuccess, message)
        {

        }
        public ResultDto(bool isSuccess, string message, T data) : base(isSuccess, message)
        {
            Data = data;
        }
        public T Data { get; set; }
    }
}
