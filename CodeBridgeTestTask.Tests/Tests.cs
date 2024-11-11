using CodeBridgeTestTask.Contexts;
using CodeBridgeTestTask.Controllers;
using CodeBridgeTestTask.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Net;
using System.Text;

namespace CodeBridgeTestTask.Tests
{
    [TestFixture]
    internal class Tests: WebApplicationFactory<Program>
    {

        private WebApplicationFactory<Program> _factory;
        private DogController _controller;
        private DogContext _context;
        private HttpClient _client;
        private DbContextOptions<DogContext> _options;
        [SetUp]
        public void Setup()
        {
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DogConnForWork");

            _options = new DbContextOptionsBuilder<DogContext>()
                .UseNpgsql(connectionString).EnableSensitiveDataLogging()
                .Options;

            _context = new DogContext(_options);
            _context.Database.EnsureCreated();

            _context.Dogs.AddRange(
                new Dog { Name = "Neo", Color = "red & amber", TailLength = 22, Weight = 32 },
                new Dog { Name = "Jessy", Color = "black & white", TailLength = 7, Weight = 14 }
            );
            _context.SaveChanges();

            _controller = new DogController(_context);

            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();

        }

        [TearDown]
        public void Teardown()
        {
            _controller?.Dispose();
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public void Ping_ShouldReturnVersionMessage()
        {
            var result = _controller.Ping() as OkObjectResult;


            Assert.AreEqual("Dogshouseservice.Version1.0.1", result.Value);
        }

        [Test]
        public async Task GetDogs_ShouldReturnAllDogs()
        {
            var result = await _controller.GetDogs() as OkObjectResult;


            var dogs = result.Value as System.Collections.Generic.List<Dog>;
            Assert.AreEqual(2, dogs.Count);
        }

        [Test]
        public async Task GetDogs_WithPagination_ShouldReturnPaginatedResults()
        {
            var result = await _controller.GetDogs(pageNumber: 2, pageSize: 1) as OkObjectResult;


            var dogs = result.Value as System.Collections.Generic.List<Dog>;
            Assert.AreEqual(1, dogs.Count);
            Assert.AreEqual("Neo", dogs[0].Name);
        }

        [Test]
        public async Task GetDogs_WithDescendingAndPagination_ShouldReturnDogsOrderedByDescAndPaginated()
        {
            var result = await _controller.GetDogs(pageNumber: 2, pageSize: 1, attribute: "taillenght", order: "desc") as OkObjectResult;

            var dogs = result.Value as List<Dog>;
            Assert.AreEqual(1, dogs.Count);
            Assert.AreEqual("Jessy", dogs[0].Name);

        }

        [Test]
        public async Task CreateDog_WithValidData_ShouldAddDog()
        {
            var newDog = new Dog { Name = "Doggy", Color = "red", TailLength = 15, Weight = 25 };
            var result = await _controller.CreateDog(newDog) as CreatedAtActionResult;


            Assert.AreEqual("Doggy", ((Dog)result.Value).Name);
        }

        [Test]
        public async Task CreateDog_WithDuplicateName_ShouldReturnBadRequest()
        {
            var duplicateDog = new Dog { Name = "Neo", Color = "green", TailLength = 20, Weight = 30 };
            var result = await _controller.CreateDog(duplicateDog) as BadRequestObjectResult;


            Assert.AreEqual("A dog with this name already exists.", result.Value);
        }

        [Test]
        public async Task GetDogs_UsingAttributeWeigth_WithDesc_ShouldReturnDogsByTheirWeightWithDescendingOrder()
        {
            var result = await _controller.GetDogs(attribute: "Weight", order: "desc") as OkObjectResult;

            var dogs = result.Value as List<Dog>;
            Assert.AreEqual("Neo", dogs[0].Name);
            Assert.AreEqual("Jessy", dogs[1].Name);
        }

        [Test]
        public async Task GetDogs_UsingAttributeWeigth_WithAsc_ShouldReturnDogsByTheirWeightWithAscendingOrder()
        {
            var result = await _controller.GetDogs(attribute: "Weight", order: "asc") as OkObjectResult;

            var dogs = result.Value as List<Dog>;
            Assert.AreEqual("Jessy", dogs[0].Name);
            Assert.AreEqual("Neo", dogs[1].Name);
        }

        [Test]
        public async Task CreateDog_WithNegativeTailLength_ShouldReturnBadRequest()
        {
            var invalidDog = new Dog { Name = "Doggy", Color = "blue", TailLength = -10, Weight = 20 };
            var result = await _controller.CreateDog(invalidDog) as BadRequestObjectResult;


            Assert.AreEqual("Tail length must be a positive number.", result.Value);
        }

        [Test]
        public async Task CreateDog_WithZeroWeight_ShouldReturnBadRequest()
        {
            var invalidDog = new Dog { Name = "Tiny", Color = "brown", TailLength = 5, Weight = 0 };
            var result = await _controller.CreateDog(invalidDog) as BadRequestObjectResult;


            Assert.AreEqual("Weight must be a positive number.", result.Value);
        }

        [Test]
        public async Task GetDogs_WithInvalidAttribute_ShouldReturnSortedByName()
        {
            var result = await _controller.GetDogs(attribute: "invalid", order: "asc") as OkObjectResult;
            var dogs = result.Value as List<Dog>;

            Assert.IsNotNull(dogs);
            Assert.AreEqual(2, dogs.Count);
            Assert.AreEqual("Jessy", dogs[0].Name);
            Assert.AreEqual("Neo", dogs[1].Name);
        }
        [Test]
        public async Task GetDogs_WithInvalidPageNumber_ShouldReturnEmptyResult()
        {
            var result = await _controller.GetDogs(pageNumber: 10, pageSize: 10) as OkObjectResult;
            var dogs = result.Value as List<Dog>;

            Assert.IsNotNull(dogs);
            Assert.AreEqual(0, dogs.Count);
        }
        [Test]
        public async Task CreateDog_WithInvalidJson_ShouldReturnNull()
        {

            var invalidDog = new Dog();

            var result = await _controller.CreateDog(invalidDog) as BadRequestResult;

            Assert.IsNull(result);

        }
        [Test]
        public async Task CreateDog_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var invalidDog = new Dog { Color = "brown" };
            var result = await _controller.CreateDog(invalidDog) as BadRequestObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
        }

        [Test]
        public async Task GetDogs_WithMixedCaseOrderAndAttribute_ShouldReturnCorrectOrder()
        {
            var result = await _controller.GetDogs(attribute: "WeIgHt", order: "AsC") as OkObjectResult;
            var dogs = result.Value as List<Dog>;

            Assert.IsNotNull(dogs);
            Assert.AreEqual("Jessy", dogs[0].Name);
            Assert.AreEqual("Neo", dogs[1].Name);
        }
        [Test]
        public async Task CreateDog_WithLargeValues_ShouldAddDogSuccessfully()
        {
            var largeDog = new Dog { Name = "BigDog", Color = "grey", TailLength = int.MaxValue, Weight = int.MaxValue };
            var result = await _controller.CreateDog(largeDog) as CreatedAtActionResult;

            Assert.AreEqual("BigDog", ((Dog)result.Value).Name);
        }

        [Test]
        public async Task GetDogs_TooManyRequests_ShouldReturn429()
        {
           
            for (int i = 0; i < 15; i++)
            {
                var response = await _client.GetAsync("http://localhost/dogs");

                if (i < 10)
                {
            
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                }
                else
                {
                    
                    Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
                }
            }
        }




    }

}
