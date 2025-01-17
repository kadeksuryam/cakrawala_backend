﻿using App.Authorization;
using App.DTOs.Requests;
using App.Helpers;
using App.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace App.Controllers
{
    [ApiController]
    [Route("voucher")]
    public class VoucherController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [Authorize(Role = "Customer")]
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var voucher = await _voucherService.GetByCode(code);

            return Ok(new SuccessDetails()
            {
                Data = voucher
            });
        }

        [Authorize(Role = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddVoucher([FromBody] AddVoucherRequestDTO reqDTO)
        {
            if(reqDTO.Code.Length != 12)
            {
                throw new HttpStatusCodeException(HttpStatusCode.BadRequest, "Invalid voucher code length");
            }

            await _voucherService.AddVoucher(reqDTO);

            return Ok(new SuccessDetails()
            {
                Data = new { message = "Add new voucher successful" }
            });
        }
    }
}
