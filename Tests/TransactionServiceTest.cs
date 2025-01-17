﻿using App.Data;
using App.DTOs.Requests;
using App.DTOs.Responses;
using App.Helpers;
using App.Models;
using App.Repositories;
using App.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TransactionServiceTest
    {
        private User mockUser = new();
        private Mock<IUserRepository> mockUserRepo = new();
        private Mock<IUserService> mockUserService = new();
        private Mock<ITransactionRepository> mockTransactionRepo = new();
        private Mock<DataContext>? mockDataContext;
        private CultureInfo? cultureInfo;
        private const DateTimeStyles dateStyles = DateTimeStyles.None;
        private Mapper? mapper;
        private TransactionService? service;
        private List<TransactionHistory> mockHistories = new();

        private void Initialize()
        {
            InitializeTransactions();
            InitializeRepositoriesAndServices();
        }

        [Fact]
        public async void GetTransactionHistoriesByUser_ValidData_ReturnSuccess()
        {
            Initialize();
            uint userId = GetAuthenticatedUserId();
            MakeMockTransactionHistoriesByUserId(userId);
            List<TransactionHistoryResponseDTO> response = await service!.GetTransactionHistoriesByUser(userId);
            AssertTransactionHistoriesResponseSortedByTime(response);
        }
        
        private void InitializeTransactions()
        {
            DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>().Options;
            mockDataContext = new Mock<DataContext>(options);
            var mockTransaction = new Mock<IDbContextTransactionProxy>();
            mockDataContext.Setup(x => x.BeginTransaction()).Returns(mockTransaction.Object);
        }

        private void InitializeRepositoriesAndServices()
        {
            mockUserRepo = new();
            mockUserService = new();
            mockTransactionRepo = new();
            AutoMapperProfile mapperProfile = new();
            MapperConfiguration mapperConfig = new(cfg =>
            {
                cfg.AddProfile(mapperProfile);
            });

            mapper = new Mapper(mapperConfig);

            service = new TransactionService(mockDataContext.Object,
                mockTransactionRepo.Object, mockUserRepo.Object, mockUserService.Object, mapper);
        }

        private uint GetAuthenticatedUserId()
        {
            uint userId = 1234;
            InitializeMockUser(userId);
            return userId;
        }

        private void InitializeMockUser(uint userId, uint balance = 0, uint exp = 0)
        {
            mockUser = new();
            mockUser.Id = userId;
            mockUser.Balance = balance;
            mockUser.Exp = exp;
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
            DateTime firstUpdateTime = response[0].UpdatedAt;

            int responseIndex = 1;
            while (responseIndex < mockHistories.Count)
            {
                DateTime secondUpdateTime = response[responseIndex].UpdatedAt;
                Assert.True(DateTime.Compare(firstUpdateTime, secondUpdateTime) > 0);

                firstUpdateTime = secondUpdateTime;
                responseIndex++;
            }
        }

        [Fact]
        public async void CreateTransaction_ValidRequest_ReturnsSuccessAsync()
        {
            Initialize();
            CreateTransactionRequestDTO request = InitializeCreateTransactionRequest();
            MakeCreateTransactionValid(request);
            CreateTransactionResponseDTO response = await CreateTransaction(request);
            AssertValidCreateTransactionResponse(request, response);
            AssertExactlyOneCreateTransactionRequestAdded();
            AssertExactlyOneAddExpRequestAdded();
        }

        [Fact]
        public async void CreateTransaction_InvalidRequest_InssuficientBalance()
        {
            Initialize();
            CreateTransactionRequestDTO request = InitializeCreateTransactionRequest();
            MakeCreateTransactionInvalid(request);
            HttpStatusCodeException exception = await Assert.ThrowsAsync<HttpStatusCodeException>(() => CreateTransaction(request));
        }

        private CreateTransactionRequestDTO InitializeCreateTransactionRequest()
        {
            CreateTransactionRequestDTO request = new()
            {
                FromUserId = 1234,
                ToUserId = 1235,
                Amount = 50000
            };

            return request;
        }

        /* Create users for making a transaction */
        private void MakeCreateTransactionValid(CreateTransactionRequestDTO request)
        {
            InitializeMockUser(request.FromUserId, 55000);
            InitializeMockUser(request.ToUserId, 5000);
        }

        /* Create an user with zero balance, so the transaction will be invalid */
        private void MakeCreateTransactionInvalid(CreateTransactionRequestDTO request)
        {
            InitializeMockUser(request.FromUserId, 0);
            InitializeMockUser(request.ToUserId, 5000);
        }

        private async Task<CreateTransactionResponseDTO> CreateTransaction(CreateTransactionRequestDTO request)
        {
            return await service!.CreateTransaction(request);
        }

        private void AssertValidCreateTransactionResponse(CreateTransactionRequestDTO request, CreateTransactionResponseDTO response)
        {
            Assert.Equal(response.From!.Id.ToString(), request.FromUserId.ToString());
            Assert.Equal(response.To!.Id.ToString(), request.ToUserId.ToString());

            Assert.Equal(response.Amount.ToString(), request.Amount.ToString());
            Assert.Equal(response.From!.CurrentBalance.ToString(), (response.From!.PreviousBalance - request.Amount).ToString());
            Assert.Equal(response.To!.CurrentBalance.ToString(), (response.To!.PreviousBalance + request.Amount).ToString());
        }

        private void AssertExactlyOneCreateTransactionRequestAdded()
        {
            mockTransactionRepo!.Verify(
                repo => repo.Add(It.IsAny<TransactionHistory>()),
                Times.Once
            );
        }

        private void AssertExactlyOneAddExpRequestAdded()
        {
            mockUserService!.Verify(
                s => s.AddExp(It.IsAny<User>(), It.Is<uint>(i => i > 0)),
                Times.Once
            );
        }
    }
}
