global using MediatR;
global using Moq;
global using TaskTracker.Application.Accounts.Queries;
global using TaskTracker.Tests.Helpers;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Query;
global using System.Linq.Expressions;
global using TaskTracker.Application.TaskReminders.Commands;

global using TaskTracker.Application.Accounts.Commands;
global using TaskTracker.Application.Common.Interfaces;
global using TaskTracker.Application.Common.Models;
global using TaskTracker.Application.Authentication.Commands;
global using System.Security.Claims;
global using TaskTracker.Application.TaskReminders.Queries;
global using TaskTracker.Domain.Constants;
global using TaskTracker.Domain.Entities;
global using TaskTracker.Domain.Events;

global using TaskTracker.Domain.Enums;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using TaskTracker.Infrastructure.Identity;