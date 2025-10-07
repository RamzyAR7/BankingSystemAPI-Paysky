using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Presentation.Controllers;
using BankingSystemAPI.Domain.Common;
using Xunit;

namespace BankingSystemAPI.UnitTests.UnitTests.Presentation
{
    public class BaseApiControllerTests
    {
        private DefaultHttpContext CreateHttpContext(string method = "GET")
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var provider = services.BuildServiceProvider();

            var ctx = new DefaultHttpContext();
            ctx.Request.Method = method;
            ctx.RequestServices = provider;
            return ctx;
        }

        private ControllerContext CreateControllerContext(string controllerName, string actionName, DefaultHttpContext ctx)
        {
            return new ControllerContext
            {
                HttpContext = ctx,
                ActionDescriptor = new ControllerActionDescriptor
                {
                    ControllerName = controllerName,
                    ActionName = actionName
                }
            };
        }

        private class TestController : BaseApiController
        {
            public IActionResult InvokeHandleResult<T>(Result<T> r) => HandleResult(r);
            public IActionResult InvokeHandleResult(Result r) => HandleResult(r);
            public IActionResult InvokeHandleCreatedResult<T>(Result<T> r, string actionName = "", object? routeValues = null) => HandleCreatedResult(r, actionName, routeValues);
            public IActionResult InvokeHandleUpdateResult<T>(Result<T> r) => HandleUpdateResult(r);
        }

        [Fact]
        public void HandleResult_Generic_Success_ReturnsOkWithValue()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("GET");
            controller.ControllerContext = CreateControllerContext("accounts", "get", ctx);

            var result = Result<string>.Success("hello");
            var actionResult = controller.InvokeHandleResult(result);

            var ok = Assert.IsType<OkObjectResult>(actionResult);
            // expect standardized envelope { success, message, data }
            var body = ok.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            var dataProp = body.GetType().GetProperty("data");
            Assert.NotNull(dataProp);
            var dataVal = dataProp.GetValue(body);
            Assert.Equal("hello", dataVal);
        }

        [Fact]
        public void HandleResult_Generic_NotFound_Returns404_And_StructuredError()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("GET");
            controller.ControllerContext = CreateControllerContext("users", "get", ctx);

            var result = Result<string>.NotFound("User not found");
            var actionResult = controller.InvokeHandleResult(result);

            var obj = Assert.IsAssignableFrom<ObjectResult>(actionResult);
            Assert.Equal(404, obj.StatusCode);

            // Validate structured error body
            var body = obj.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            var msgProp = body.GetType().GetProperty("message");
            Assert.NotNull(msgProp);
            var message = msgProp.GetValue(body) as string;
            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.Contains("User", message, StringComparison.OrdinalIgnoreCase);

            var errorsProp = body.GetType().GetProperty("errors");
            Assert.NotNull(errorsProp);
            var errorsVal = errorsProp.GetValue(body) as System.Collections.IEnumerable;
            Assert.NotNull(errorsVal);

            var first = errorsVal.Cast<object>().FirstOrDefault();
            Assert.NotNull(first);
            var typeProp = first.GetType().GetProperty("type");
            Assert.NotNull(typeProp);
            var typeVal = typeProp.GetValue(first) as string;
            Assert.Equal("NotFound", typeVal);
        }

        [Fact]
        public void HandleCreatedResult_NoActionName_Returns201_WithValue()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("POST");
            controller.ControllerContext = CreateControllerContext("banks", "create", ctx);

            var result = Result<int>.Success(123);
            var actionResult = controller.InvokeHandleCreatedResult(result);

            var obj = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(201, obj.StatusCode);
            var body = obj.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            var dataProp = body.GetType().GetProperty("data");
            Assert.NotNull(dataProp);
            var dataVal = dataProp.GetValue(body);
            Assert.Equal(123, dataVal);
        }

        [Fact]
        public void HandleCreatedResult_WithActionName_Returns_CreatedAtAction()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("POST");
            controller.ControllerContext = CreateControllerContext("banks", "create", ctx);

            var result = Result<int>.Success(999);
            var actionResult = controller.InvokeHandleCreatedResult(result, "GetById", new { id = 999 });

            var created = Assert.IsType<CreatedAtActionResult>(actionResult);
            Assert.Equal("GetById", created.ActionName);
            var body = created.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            var dataProp = body.GetType().GetProperty("data");
            Assert.NotNull(dataProp);
            var dataVal = dataProp.GetValue(body);
            Assert.Equal(999, dataVal);
        }

        [Fact]
        public void HandleUpdateResult_PasswordAction_ReturnsMessageOnly_NoData()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("PUT");
            controller.ControllerContext = CreateControllerContext("users", "ChangePassword", ctx);

            var result = Result<string>.Success("should-not-be-returned");
            var actionResult = controller.InvokeHandleUpdateResult(result);

            var ok = Assert.IsType<OkObjectResult>(actionResult);
            var body = ok.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            // For password/status updates we return message-only object without `data`
            var dataProp = body.GetType().GetProperty("data");
            Assert.Null(dataProp);
        }

        [Fact]
        public void HandleResult_InsufficientFunds_ReturnsConflict_And_StructuredError()
        {
            var controller = new TestController();
            var ctx = CreateHttpContext("POST");
            controller.ControllerContext = CreateControllerContext("transactions", "withdraw", ctx);

            var result = Result<int>.InsufficientFunds(200m, 100m);
            var actionResult = controller.InvokeHandleResult(result);

            var obj = Assert.IsAssignableFrom<ObjectResult>(actionResult);
            Assert.Equal(409, obj.StatusCode);

            var body = obj.Value ?? throw new Xunit.Sdk.XunitException("Response body was null");
            var errorsProp = body.GetType().GetProperty("errors");
            Assert.NotNull(errorsProp);
            var errorsVal = errorsProp.GetValue(body) as System.Collections.IEnumerable;
            Assert.NotNull(errorsVal);
            var first = errorsVal.Cast<object>().FirstOrDefault();
            Assert.NotNull(first);
            var typeProp = first.GetType().GetProperty("type");
            Assert.NotNull(typeProp);
            var typeVal = typeProp.GetValue(first) as string;
            Assert.Equal("BusinessRule", typeVal);
        }
    }
}
