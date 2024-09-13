using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static APIPetrack.Models.Users.PetStoreShelter;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetStoreShelterController : Controller
    {
        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;

        public PetStoreShelterController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(PetStoreShelter petStoreShelter)
        {

            if (petStoreShelter == null || !ModelState.IsValid)
            {
                return BadRequest(petStoreShelter == null ? "Invalid pet store shelter data." : ModelState);
            }

            try
            {
                if (await _context.PetOwner.AnyAsync(po => po.Email == petStoreShelter.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                petStoreShelter.Password = _passwordHasher.HashPassword(petStoreShelter.Password);

                _context.PetStoreShelter.Add(petStoreShelter);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Account created successfully." });
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is Exception)
            {
                var errorMessage = ex is DbUpdateException ? "An error occurred while creating the account." : "An unexpected error occurred.";
                return StatusCode(500, new { message = errorMessage, details = ex.Message });
            }

        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] PetStoreShelter.LoginPetStoreShelter loginPetStoreShelter)
        {
            var response = await _authorizationService.LoginPetShoreShelterAsync(loginPetStoreShelter);

            if (response.Result)
            {
                var petStoreShelter = await _context.PetStoreShelter.FirstOrDefaultAsync(po => po.Email == loginPetStoreShelter.Email);

                if (petStoreShelter != null)
                {
                    var result = new
                    {
                        response.Result,
                        response.Token,
                        PetStoreShelterId = petStoreShelter.Id,
                        PetStoreShelterName = petStoreShelter.Name,
                        PetStoreShelterEmail = petStoreShelter.Email,
                        PetStoreShelterAddress = petStoreShelter.Address,
                    };

                    return Ok(result);
                }

                return Unauthorized(new { message = "Invalid login credentials." });
            }
            else
            {
                return Unauthorized(response);
            }
        }

    }
}
