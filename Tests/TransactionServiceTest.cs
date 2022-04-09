﻿using App.Data;
using App.DTOs.Responses;
using App.Helpers;
using App.Models;
using App.Repositories;
using App.Services;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class TransactionServiceTest
    {
        private User mockUser = new();
        private Mock<IUserRepository> mockUserRepo = new();
        private Mock<ITransactionRepository> mockTransactionRepo = new();
        private Mock<IDataContext>? mockDataContext;
        private Mapper? mapper;
        private TransactionService? service;
        private List<TransactionHistory> mockHistories = new();

        [Fact]
        public async void GetTransactionHistoriesByUser_ValidData_ReturnSuccess()
        {
            InitializeRepositoriesAndServices();
            uint userId = GetAuthenticatedUserId();
            MakeMockTransactionHistoriesByUserId(userId);
            List<TransactionHistoryResponseDTO> response = await service!.GetTransactionHistoriesByUser(userId);
            AssertTransactionHistoriesResponseSortedByTime(response);
        }

        private void InitializeRepositoriesAndServices()
        {
            mockUserRepo = new();
            mockTransactionRepo = new();
            mockDataContext = new();

            MapperConfiguration mapperConfig = new(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile());
            });

            mapper = new Mapper(mapperConfig);

            service = new TransactionService(mockDataContext.Object,
                mockTransactionRepo.Object, mockUserRepo.Object, mapper);
        }

        private uint GetAuthenticatedUserId()
        {
            uint userId = 1234;
            MakeUserIdAuthenticated(userId);
            return userId;
        }

        private void MakeUserIdAuthenticated(uint userId)
        {
            mockUser = new();
            mockUser.Id = userId;
            mockUser.Balance = 0;
            mockUserRepo!.Setup(repo => repo.GetById(userId)).ReturnsAsync(mockUser);
        }

        private void MakeMockTransactionHistoriesByUserId(uint userId)
        {
            mockHistories = new();
            mockHistories.Add(new TransactionHistory()
            {
                UpdatedAt = DateTime.Now
            });
            mockHistories.Add(new TransactionHistory()
            {
                UpdatedAt = DateTime.Now.AddHours(2)
            });
            mockHistories.Add(new TransactionHistory()
            {
                UpdatedAt = DateTime.Now.AddHours(1)
            });

            mockTransactionRepo.Setup(repo => repo.GetAllByUserId(userId)).ReturnsAsync(mockHistories);
        }

        private void AssertTransactionHistoriesResponseSortedByTime(List<TransactionHistoryResponseDTO> response)
        {
            // Later first
            Assert.True(DateTime.TryParse(
                response[0].UpdatedAt,
                out DateTime firstUpdateTime
            ));

            int responseIndex = 1;
            while (responseIndex < mockHistories.Count)
            {
                Assert.True(DateTime.TryParse(
                    response[responseIndex].UpdatedAt,
                    out DateTime secondUpdateTime
                ));
                Assert.True(DateTime.Compare(firstUpdateTime, secondUpdateTime) > 0);

                firstUpdateTime = secondUpdateTime;
                responseIndex++;
            }
        }
    }
}
