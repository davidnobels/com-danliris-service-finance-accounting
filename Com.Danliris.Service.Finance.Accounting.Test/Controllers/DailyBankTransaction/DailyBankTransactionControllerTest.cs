﻿using AutoMapper;
using Com.Danliris.Service.Finance.Accounting.Lib.BusinessLogic.Interfaces.DailyBankTransaction;
using Com.Danliris.Service.Finance.Accounting.Lib.Models.DailyBankTransaction;
using Com.Danliris.Service.Finance.Accounting.Lib.Services.IdentityService;
using Com.Danliris.Service.Finance.Accounting.Lib.Services.ValidateService;
using Com.Danliris.Service.Finance.Accounting.Lib.Utilities;
using Com.Danliris.Service.Finance.Accounting.Lib.ViewModels.DailyBankTransaction;
using Com.Danliris.Service.Finance.Accounting.Test.Controller.Utils;
using Com.Danliris.Service.Finance.Accounting.WebApi.Controllers.v1.DailyBankTransaction;
using Com.Moonlay.NetCore.Lib.Service;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Com.Danliris.Service.Finance.Accounting.Test.Controllers.DailyBankTransaction
{
    public class DailyBankTransactionControllerTest : BaseControllerTest<DailyBankTransactionController, DailyBankTransactionModel, DailyBankTransactionViewModel, IDailyBankTransactionService>
    {
        private async Task<int> GetStatusCodeDeleteByReferenceNo((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IDailyBankTransactionService> Service, Mock<IMapper> Mapper) mocks)
        {
            DailyBankTransactionController controller = GetController(mocks);
            IActionResult response = await controller.DeleteByReferenceNo("1");
            return GetStatusCode(response);
        }

        [Fact]
        public async Task DeleteByReferenceNo_WithoutException_ReturnNoContent()
        {
            var mocks = GetMocks();
            mocks.Service.Setup(f => f.DeleteByReferenceNoAsync(It.IsAny<string>())).ReturnsAsync(1);

            int statusCode = await GetStatusCodeDeleteByReferenceNo(mocks);
            Assert.Equal((int)HttpStatusCode.NoContent, statusCode);
        }

        [Fact]
        public async Task DeleteByReferenceNo_ThrowException_ReturnInternalStatusError()
        {
            var mocks = GetMocks();
            mocks.Service.Setup(f => f.DeleteByReferenceNoAsync(It.IsAny<string>())).ThrowsAsync(new Exception());

            int statusCode = await GetStatusCodeDeleteByReferenceNo(mocks);
            Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public void GetReport_Without_Exception()
        {
            var mocks = GetMocks();
            mocks.Service.Setup(f => f.GetReport(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(new ReadResponse<DailyBankTransactionModel>(new List<DailyBankTransactionModel>(), 0, new Dictionary<string, string>(), new List<string>()));
            mocks.Mapper.Setup(f => f.Map<List<DailyBankTransactionViewModel>>(It.IsAny<List<DailyBankTransactionModel>>())).Returns(ViewModels);

            int statusCode = GetStatusCodeGetReport(mocks);
            Assert.Equal((int)HttpStatusCode.OK, statusCode);
        }

        private int GetStatusCodeGetReport((Mock<IIdentityService> IdentityService, Mock<IValidateService> ValidateService, Mock<IDailyBankTransactionService> Service, Mock<IMapper> Mapper) mocks)
        {
            var controller = GetController(mocks);
            IActionResult response = controller.GetReport(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>());

            return GetStatusCode(response);
        }
    }
}