﻿using System;
using FakeItEasy;
using Tweetinvi.Controllers.User;
using Tweetinvi.Core.QueryGenerators;
using Tweetinvi.Core.QueryValidators;
using Tweetinvi.Models.DTO;
using Tweetinvi.Parameters;
using Xunit;
using xUnitinvi.TestHelpers;

namespace xUnitinvi.TweetinviControllers.UserTests
{
    public class UserQueryGeneratorTests
    {
        private readonly FakeClassBuilder<UserQueryGenerator> _fakeBuilder;
        private readonly Fake<IUserQueryParameterGenerator> _fakeUserQueryParameterGenerator;
        private readonly Fake<IUserQueryValidator> _fakeUserQueryValidator;

        public UserQueryGeneratorTests()
        {
            _fakeBuilder = new FakeClassBuilder<UserQueryGenerator>();
            _fakeUserQueryParameterGenerator = _fakeBuilder.GetFake<IUserQueryParameterGenerator>();
            _fakeUserQueryValidator = _fakeBuilder.GetFake<IUserQueryValidator>();
        }

        private UserQueryGenerator CreateUserQueryGenerator()
        {
            return _fakeBuilder.GenerateClass();
        }

        [Fact]
        public void GetBlockUserQuery_WithValidUserDTO_ReturnsExpectedQuery()
        {
            // Arrange
            var queryGenerator = CreateUserQueryGenerator();
            var userDTO = GenerateUserDTO(true);

            var parameters = new BlockUserParameters(userDTO)
            {
                SkipStatus = true,
                IncludeEntities = true
            };

            // Act
            var result = queryGenerator.GetBlockUserQuery(parameters);

            // Assert
            Assert.Equal(result, $"https://api.twitter.com/1.1/blocks/create.json?user_id=42&include_entities=true&skip_status=true");

            _fakeUserQueryValidator.CallsTo(x => x.ThrowIfUserCannotBeIdentified(userDTO)).MustHaveHappened();
        }

        private IUserDTO GenerateUserDTO(bool isValid)
        {
            var userDTO = A.Fake<IUserDTO>();
            userDTO.Id = 42;

            _fakeUserQueryValidator.CallsTo(x => x.CanUserBeIdentified(userDTO)).Returns(isValid);
            _fakeUserQueryParameterGenerator.CallsTo(x => x.GenerateIdOrScreenNameParameter(userDTO, "user_id", "screen_name")).Returns("user_id=42");

            if (!isValid)
            {
                _fakeUserQueryValidator.CallsTo(x => x.ThrowIfUserCannotBeIdentified(userDTO)).Throws(new ArgumentException());
                _fakeUserQueryValidator.CallsTo(x => x.ThrowIfUserCannotBeIdentified(userDTO, It.IsAny<string>())).Throws(new ArgumentException());
            }

            return userDTO;
        }
    }
}
