using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for DeleteUser request.
    /// </summary>
    /// <param name="roomRepository">Implementation of <see cref="IRoomRepository"/> for operating with database.</param>
    /// <param name="userReadOnlyRepository">Implementation of <see cref="IUserReadOnlyRepository"/> for operating with database.</param>
    public class DeleteUserHandler(
        IRoomRepository roomRepository,
        IUserReadOnlyRepository userReadOnlyRepository)
        : IRequestHandler<DeleteUserRequest, Result<RoomAggregate, ValidationResult>>
    {
        ///<inheritdoc/>
        public async Task<Result<RoomAggregate, ValidationResult>> Handle(DeleteUserRequest request,
            CancellationToken cancellationToken)
        {
            var userByIdResult = await userReadOnlyRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (userByIdResult.IsFailure)
            {
                return userByIdResult.Error;
            }

            var userByCodeResult =
                await userReadOnlyRepository.GetByCodeAsync(request.UserCode, cancellationToken, includeRoom: true);
            if (userByCodeResult.IsFailure)
            {
                return userByCodeResult.Error;
            }

            var userByCode = userByCodeResult.Value;
            if (!userByCode.IsAdmin)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new ForbiddenError([
                    new ValidationFailure(string.Empty, "User with such code is not admin.")
                ]));
            }

            var userById = userByIdResult.Value;
            if (userById.Id == userByCode.Id)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, "User with such code and such id is the same user.")
                ]));
            }

            if (userById.RoomId != userByCode.RoomId)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, "Users with such code and such id are not in the same room.")
                ]));
            }

            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return roomResult;
            }

            var room = roomResult.Value;

            var deleteResult = room.DeleteUser(request.UserId);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            var updatedResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updatedResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, updatedResult.Error)
                ]));
            }

            var updatedRoomResult = await roomRepository.GetByRoomCodeAsync(room.InvitationCode, cancellationToken);
            return updatedRoomResult;
        }
    }
}