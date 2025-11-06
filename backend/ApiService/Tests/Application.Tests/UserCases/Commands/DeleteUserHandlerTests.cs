using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands;

/// <summary>
/// Unit tests for the <see cref="DeleteUserHandler"/> class.
/// </summary>
public class DeleteUserHandlerTests
{
    private readonly IRoomRepository _roomRepositoryMock;
    private readonly IUserReadOnlyRepository _userReadOnlyRepositoryMock;
    private readonly DeleteUserHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteUserHandlerTests"/> class with mocked dependencies.
    /// </summary>
    public DeleteUserHandlerTests()
    {
        _roomRepositoryMock = Substitute.For<IRoomRepository>();
        _userReadOnlyRepositoryMock = Substitute.For<IUserReadOnlyRepository>();
        _handler = new DeleteUserHandler(_roomRepositoryMock, _userReadOnlyRepositoryMock);
    }

    /// <summary>
    /// Tests that the handler returns a <see cref="NotFoundError"/> when the specified user is not found by id.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserByIdNotFound()
    {
        // Arrange
        var fakeUser = DataFakers.UserFaker.Generate();
        var request = new DeleteUserRequest(fakeUser.AuthCode, fakeUser.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(Arg.Any<ulong>(), CancellationToken.None)
            .Returns(new NotFoundError([
                new ValidationFailure(string.Empty, "User with such id not found")
            ]));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("User with such id not found"));
    }
    
    /// <summary>
    /// Tests that the handler returns a <see cref="NotFoundError"/> when the specified user is not found by code.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserByCodeNotFound()
    {
        // Arrange
        var fakeUser = DataFakers.UserFaker.Generate();
        var request = new DeleteUserRequest(fakeUser.AuthCode, fakeUser.Id);

        _userReadOnlyRepositoryMock
            .GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Failure<User, ValidationResult>(
                new NotFoundError([
                    new ValidationFailure(string.Empty, "User with such code not found")
                ])
            ));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("User with such code not found"));
    }
    
    /// <summary>
    /// Tests that the handler returns a <see cref="ForbiddenError"/> when the user by code is not admin.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserWithCodeIsNotAdmin()
    {
        // Arrange
        var fakeUserById = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => false)
            .Generate();
        var fakeUserByCode = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)2)
            .RuleFor(u => u.IsAdmin, _ => false)
            .Generate();

        var request = new DeleteUserRequest(fakeUserByCode.AuthCode, fakeUserById.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(fakeUserById.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserById));
        
        _userReadOnlyRepositoryMock
            .GetByCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserByCode));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ForbiddenError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("User with such code is not admin."));
    }
    
    /// <summary>
    /// Tests that the handler returns a <see cref="BadRequestError"/> when users with such code and such id are not in the same room.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUsersWithIdAndCodeAreNotInTheSameRoom()
    {
        // Arrange
        var fakeUserById = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => false)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeUserByCode = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)2)
            .RuleFor(u => u.IsAdmin, _ => true)
            .RuleFor(u => u.RoomId, _ => (ulong)20)
            .Generate();

        var request = new DeleteUserRequest(fakeUserByCode.AuthCode, fakeUserById.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(fakeUserById.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserById));
        
        _userReadOnlyRepositoryMock
            .GetByCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserByCode));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BadRequestError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("Users with such code and such id are not in the same room."));
    }
    
    /// <summary>
    /// Tests that the handler returns a <see cref="BadRequestError"/> when user with such code and such id is the same user.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserWithIdAndCodeIsTheSame()
    {
        // Arrange
        var fakeUserById = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => true)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeUserByCode = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => true)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();

        var request = new DeleteUserRequest(fakeUserByCode.AuthCode, fakeUserById.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(fakeUserById.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserById));
        
        _userReadOnlyRepositoryMock
            .GetByCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserByCode));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BadRequestError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("User with such code and such id is the same user."));
    }
    
    /// <summary>
    /// Tests that the handler returns a <see cref="BadRequestError"/> when room is already closed.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRoomAlreadyClosed()
    {
        // Arrange
        var fakeUserById = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => false)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeUserByCode = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)2)
            .RuleFor(u => u.IsAdmin, _ => true)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeRoom = DataFakers.RoomFaker
            .RuleFor(r => r.Id, _ => (ulong)10)
            .RuleFor(r => r.Users, _ => [fakeUserById, fakeUserByCode])
            .RuleFor(r => r.ClosedOn, _ => DateTime.MinValue);

        var request = new DeleteUserRequest(fakeUserByCode.AuthCode, fakeUserById.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(fakeUserById.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserById));
        
        _userReadOnlyRepositoryMock
            .GetByCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserByCode));

        _roomRepositoryMock
            .GetByUserCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>())
            .Returns(Result.Success<Room, ValidationResult>(fakeRoom));
        
        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BadRequestError>();
        result.Error.Errors.Should().Contain(error =>
            error.ErrorMessage.Equals("Room is already closed."));
    }

    /// <summary>
    /// Tests that the handler successfully delete user when provided with valid user id and user code.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenProvidedValidInputData()
    {
        // Arrange
        var fakeUserById = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)1)
            .RuleFor(u => u.IsAdmin, _ => false)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeUserByCode = DataFakers.UserFaker
            .RuleFor(u => u.Id, _ => (ulong)2)
            .RuleFor(u => u.IsAdmin, _ => true)
            .RuleFor(u => u.RoomId, _ => (ulong)10)
            .Generate();
        var fakeRoom = DataFakers.RoomFaker
            .RuleFor(r => r.InvitationCode, _ => "d275c851c4624f11b7252ffd8b0e1dab")
            .RuleFor(r => r.Id, _ => (ulong)10)
            .RuleFor(r => r.Users, _ => [fakeUserById, fakeUserByCode])
            .RuleFor(r => r.ClosedOn, _ => null);
        var fakeRoomAfterUserSuccessfullyDeleted = DataFakers.RoomFaker
            .RuleFor(r => r.Id, _ => (ulong)10)
            .RuleFor(r => r.Users, _ => [fakeUserByCode])
            .RuleFor(r => r.ClosedOn, _ => null);

        var request = new DeleteUserRequest(fakeUserByCode.AuthCode, fakeUserById.Id);

        _userReadOnlyRepositoryMock
            .GetByIdAsync(fakeUserById.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserById));
        
        _userReadOnlyRepositoryMock
            .GetByCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(Result.Success<User, ValidationResult>(fakeUserByCode));

        _roomRepositoryMock
            .GetByUserCodeAsync(fakeUserByCode.AuthCode, Arg.Any<CancellationToken>())
            .Returns(Result.Success<Room, ValidationResult>(fakeRoom));
        
        _roomRepositoryMock
            .GetByRoomCodeAsync("d275c851c4624f11b7252ffd8b0e1dab", Arg.Any<CancellationToken>())
            .Returns(Result.Success<Room, ValidationResult>(fakeRoomAfterUserSuccessfullyDeleted));
        
        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _roomRepositoryMock.Received(1).UpdateAsync(
            Arg.Is<Room>(r => r.Users.All(u => u.Id != fakeUserById.Id)),
            Arg.Any<CancellationToken>()
        );
    }
}