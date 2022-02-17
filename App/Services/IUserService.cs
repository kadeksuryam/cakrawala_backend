﻿using App.DTOs.Requests;
using App.DTOs.Responses;

namespace App.Services
{
    public interface IUserService
    {
        Task Register(RegisterRequestDTO dto);
        Task<LoginResponseDTO> Login(LoginRequestDTO dto);
    }
}
