﻿using App.Models;

namespace App.Repositories
{
    public interface IBankRepository
    {
        Task<Bank?> GetById(uint id);
        Task<IEnumerable<Bank>> GetAll(List<uint> bankIds);
    }
}
