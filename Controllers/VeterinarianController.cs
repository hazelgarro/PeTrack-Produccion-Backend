﻿using APIPetrack.Context;
using APIPetrack.Models.Custom;
using APIPetrack.Models.Users;
using APIPetrack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIPetrack.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VeterinarianController : Controller
    {

        private readonly DbContextPetrack _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuthorizationServices _authorizationService;
        public VeterinarianController(DbContextPetrack pContext, IPasswordHasher passwordHasher, IAuthorizationServices authorizationService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount(Veterinarian veterinarian)
        {

            if (veterinarian == null || !ModelState.IsValid)
            {
                return BadRequest(veterinarian == null ? "Invalid veterinarian data." : ModelState);
            }

            try
            {
                if (await _context.PetOwner.AnyAsync(po => po.Email == veterinarian.Email))
                {
                    return Conflict(new { message = "Email already in use." });
                }

                veterinarian.Password = _passwordHasher.HashPassword(veterinarian.Password);

                _context.Veterinarian.Add(veterinarian);
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
        public async Task<IActionResult> Login([FromBody] Veterinarian.LoginVeterinarian loginVeterinarian)
        {
            var response = await _authorizationService.LoginVeterinarianAsync(loginVeterinarian);

            if (response.Result)
            {
                var veterinarian = await _context.Veterinarian.FirstOrDefaultAsync(v => v.Email == loginVeterinarian.Email);

                if (veterinarian != null)
                {
                    var result = new
                    {
                        response.Result,
                        response.Token,
                        VeterinarianId = veterinarian.Id,
                        VeterinarianFirstName = veterinarian.FirstName,
                        VeterinarianLastName = veterinarian.LastName,
                        VeterinarianEmail = veterinarian.Email,
                        VeterinarianClinic = veterinarian.ClinicName,
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
