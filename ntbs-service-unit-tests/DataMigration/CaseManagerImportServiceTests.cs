﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using ntbs_service.DataAccess;
using ntbs_service.DataMigration;
using ntbs_service.DataMigration.RawModels;
using ntbs_service.Models.Entities;
using ntbs_service.Models.ReferenceEntities;
using ntbs_service.Properties;
using ntbs_service.Services;
using Xunit;

namespace ntbs_service_unit_tests.DataMigration
{
    public class CaseManagerImportServiceTests : IDisposable
    {
        private const int BATCH_ID = 56789;
        private const string NOTIFICATION_ID = "11111";
        private const string CASE_MANAGER_USERNAME_1 = "TestUser@nhs.net";
        private const string CASE_MANAGER_USERNAME_2 = "MartinUser@nhs.net";
        private static readonly Guid HOSPITAL_GUID_1 = new Guid("B8AA918D-233F-4C41-B9AE-BE8A8DC8BE7A");
        private static readonly Guid HOSPITAL_GUID_2 = new Guid("B8AA918D-233F-4C41-B9AE-BE8A8DC8BE7B");

        private readonly NtbsContext _context;
        private readonly CaseManagerImportService _caseManagerImportService;
        private readonly Mock<IMigrationRepository> _migrationRepositoryMock = new Mock<IMigrationRepository>();
        private readonly Mock<IOptionsMonitor<AdOptions>> _adOptionMock = new Mock<IOptionsMonitor<AdOptions>>();

        private Dictionary<string, IEnumerable<MigrationDbNotification>> _idToNotificationDict;
        private Dictionary<string, MigrationLegacyUser> _usernameToLegacyUserDict = new Dictionary<string, MigrationLegacyUser>();
        private Dictionary<string, IEnumerable<MigrationLegacyUserHospital>> _usernameToLegacyUserHospitalDict = new Dictionary<string, IEnumerable<MigrationLegacyUserHospital>>();

        public CaseManagerImportServiceTests()
        {
            _context = SetupTestContext();
            SetupMockMigrationRepo();
            _adOptionMock.Setup(s => s.CurrentValue).Returns(new AdOptions{ReadOnlyUserGroup = "TestReadOnly"});
            IUserRepository userRepository = new UserRepository(_context, _adOptionMock.Object);
            IReferenceDataRepository referenceDataRepository = new ReferenceDataRepository(_context);
            Mock<INotificationImportRepository> mockNotificationImportRepository = new Mock<INotificationImportRepository>();

            var importLogger = new ImportLogger(mockNotificationImportRepository.Object);
            _caseManagerImportService = new CaseManagerImportService(userRepository, referenceDataRepository,
                _migrationRepositoryMock.Object, importLogger);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task WhenCaseManagerForLegacyNotificationWithCorrectPermissionsDoesNotExistInNtbs_AddsCaseManagerWithTbServices()
        {
            // Arrange
            var notification = GivenLegacyNotificationWithCaseManagerAndTbServiceCode(CASE_MANAGER_USERNAME_1, "TBS00TEST");
            GivenLegacyUserWithName(CASE_MANAGER_USERNAME_1, "John", "Johnston");
            await GivenLegacyUserHasPermissionsForTbServiceInHospital(CASE_MANAGER_USERNAME_1, "TBS00TEST", HOSPITAL_GUID_1);

            // Act
            await _caseManagerImportService.ImportOrUpdateUserFromNotification(notification, null, BATCH_ID);

            // Assert
            var addedUser = _context.User.SingleOrDefault();
            Assert.NotNull(addedUser);
            Assert.Equal("John", addedUser.GivenName);
            Assert.Equal("Johnston", addedUser.FamilyName);
            Assert.False(addedUser.IsActive);
            Assert.True(addedUser.IsCaseManager);
            Assert.Contains("TBS00TEST", addedUser.CaseManagerTbServices.Select(cmtb => cmtb.TbServiceCode));
        }

        [Fact]
        public async Task WhenCaseManagerForLegacyNotificationWithIncorrectPermissionsDoesNotExistInNtbs_AddsCaseManagerWithNoTbServices()
        {
            // Arrange
            var notification = GivenLegacyNotificationWithCaseManagerAndTbServiceCode(CASE_MANAGER_USERNAME_1, "TBS00TEST");
            GivenLegacyUserWithName(CASE_MANAGER_USERNAME_1, "John", "Johnston");
            await GivenLegacyUserHasPermissionsForTbServiceInHospital(CASE_MANAGER_USERNAME_1, "TBS11FAKE", HOSPITAL_GUID_1);

            // Act
            await _caseManagerImportService.ImportOrUpdateUserFromNotification(notification, null, BATCH_ID);

            // Assert
            var addedUser = _context.User.SingleOrDefault();
            Assert.NotNull(addedUser);
            Assert.Equal("John", addedUser.GivenName);
            Assert.Equal("Johnston", addedUser.FamilyName);
            Assert.False(addedUser.IsActive);
            Assert.False(addedUser.IsCaseManager);
            Assert.DoesNotContain("TBS00TEST", addedUser.CaseManagerTbServices.Select(cmtb => cmtb.TbServiceCode));
        }

        [Fact]
        public async Task WhenCaseManagerForLegacyNotificationExistsInNtbs_UserNotImportedButNameUpdated()
        {
            // Arrange
            var notification = GivenLegacyNotificationWithCaseManagerAndTbServiceCode(CASE_MANAGER_USERNAME_1, "TBS00TEST");
            GivenLegacyUserWithName(CASE_MANAGER_USERNAME_1, "John", "Johnston");
            await GivenLegacyUserHasPermissionsForTbServiceInHospital(CASE_MANAGER_USERNAME_1, "TBS99HULL", HOSPITAL_GUID_1);
            await GivenUserExistsInNtbsWithName("Jon", "Jonston");

            // Act
            await _caseManagerImportService.ImportOrUpdateUserFromNotification(notification, null, BATCH_ID);

            // Assert
            var updatedUser = _context.User.Single();
            Assert.NotNull(updatedUser);
            Assert.Equal("John", updatedUser.GivenName);
            Assert.Equal("Johnston", updatedUser.FamilyName);
        }

        private void SetupMockMigrationRepo()
        {
            _migrationRepositoryMock.Setup(repo => repo.GetLegacyUserByUsername(It.IsAny<string>()))
                .Returns((string username) => Task.FromResult(_usernameToLegacyUserDict[username]));
            _migrationRepositoryMock.Setup(repo => repo.GetLegacyUserHospitalsByUsername(It.IsAny<string>()))
                .Returns((string username) => Task.FromResult(_usernameToLegacyUserHospitalDict[username]));
            _migrationRepositoryMock.Setup(repo => repo.GetNotificationsById(It.IsAny<List<string>>()))
                .Returns((List<string> ids) => Task.FromResult(_idToNotificationDict[ids[0]]));
        }

        private Notification GivenLegacyNotificationWithCaseManagerAndTbServiceCode(string caseManager, string TbServiceCode)
        {
            _idToNotificationDict = new Dictionary<string, IEnumerable<MigrationDbNotification>>
            {
                {
                    NOTIFICATION_ID,
                    new List<MigrationDbNotification> {new MigrationDbNotification {CaseManager = caseManager}}
                }
            };
            return new Notification
            {
                IsLegacy = true,
                LegacySource = "LTBR",
                LTBRID = NOTIFICATION_ID,
                HospitalDetails = new HospitalDetails {TBServiceCode = TbServiceCode},
                TreatmentEvents = new List<TreatmentEvent>()
            };
        }

        private Notification GivenLegacyNotificationWithNoCaseManager()
        {
            _idToNotificationDict = new Dictionary<string, IEnumerable<MigrationDbNotification>>
            {
                {
                    NOTIFICATION_ID,
                    new List<MigrationDbNotification> {new MigrationDbNotification()}
                }
            };
            return new Notification
            {
                IsLegacy = true,
                LTBRID = NOTIFICATION_ID,
                HospitalDetails = new HospitalDetails(),
                TreatmentEvents = new List<TreatmentEvent>()
            };
        }

        private void GivenLegacyUserWithName(string username, string givenName, string familyName)
        {
            _usernameToLegacyUserDict.Add(
                username,
                new MigrationLegacyUser
                {
                    Username = username, GivenName = givenName, FamilyName = familyName
                }
            );
        }

        private async Task GivenLegacyUserHasPermissionsForTbServiceInHospital(string username, string tbServiceCode, Guid hospitalGuid)
        {
            _usernameToLegacyUserHospitalDict.Add(
                username,
                new List<MigrationLegacyUserHospital> {new MigrationLegacyUserHospital {HospitalId = hospitalGuid}}
            );
            await _context.Hospital.AddAsync(new Hospital{HospitalId = hospitalGuid, TBService = new TBService{Code = tbServiceCode}});
            await _context.SaveChangesAsync();
        }

        private async Task GivenUserExistsInNtbsWithName(string givenName, string familyName)
        {
            await _context.User.AddAsync(
                new User {GivenName = givenName, FamilyName = familyName, Username = CASE_MANAGER_USERNAME_1, IsActive = true});
            await _context.SaveChangesAsync();
        }

        private NtbsContext SetupTestContext()
        {
            // Generating a unique database name makes sure the database is not shared between tests.
            string dbName = Guid.NewGuid().ToString();
            return new NtbsContext(new DbContextOptionsBuilder<NtbsContext>()
                .UseInMemoryDatabase(dbName)
                .Options
            );
        }
    }
}
