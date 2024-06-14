using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using JWT.Contexts;
using JWT.Exceptions;
using JWT.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JWT.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IConfiguration config, DatabaseContext context) : ControllerBase
{
    public IActionResult Login(LoginRegisterRequest model)
    {
        if (!(model.Email.ToLower() == "mail@gmail.com" && model.Password == "pass"))
        {
            return Unauthorized("Wrong username or password");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescription = new SecurityTokenDescriptor
        {
            Issuer = config["JWT:Issuer"],
            Audience = config["JWT:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!)),
                SecurityAlgorithms.HmacSha256
            )
        };
        var token = tokenHandler.CreateToken(tokenDescription);
        var stringToken = tokenHandler.WriteToken(token);

        var refTokenDescription = new SecurityTokenDescriptor
        {
            Issuer = config["JWT:RefIssuer"],
            Audience = config["JWT:RefAudience"],
            Expires = DateTime.UtcNow.AddDays(3),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:RefKey"]!)),
                SecurityAlgorithms.HmacSha256
            )
        };
        var refToken = tokenHandler.CreateToken(refTokenDescription);
        var stringRefToken = tokenHandler.WriteToken(refToken);

        return Ok(new LoginResponseModel
        {
            Token = stringToken,
            RefreshToken = stringRefToken
        });
    }

    [HttpPost("/auth/refresh")]
    public IActionResult RefreshToken(RefreshTokenRequestModel requestModel)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(requestModel.RefreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["JWT:RefIssuer"],
                ValidAudience = config["JWT:RefAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:RefKey"]!))
            }, out SecurityToken validatedToken);
            // return Ok(true + " " + validatedToken);

            // generowanie kolejnego tokenu oraz refresh tokenu

            tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescription = new SecurityTokenDescriptor
            {
                Issuer = config["JWT:Issuer"],
                Audience = config["JWT:Audience"],
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!)),
                    SecurityAlgorithms.HmacSha256
                )
            };
            var token = tokenHandler.CreateToken(tokenDescription);
            var stringToken = tokenHandler.WriteToken(token);

            var refTokenDescription = new SecurityTokenDescriptor
            {
                Issuer = config["JWT:RefIssuer"],
                Audience = config["JWT:RefAudience"],
                Expires = DateTime.UtcNow.AddDays(3),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:RefKey"]!)),
                    SecurityAlgorithms.HmacSha256
                )
            };
            var refToken = tokenHandler.CreateToken(refTokenDescription);
            var stringRefToken = tokenHandler.WriteToken(refToken);

            return Ok(new LoginResponseModel
            {
                Token = stringToken,
                RefreshToken = stringRefToken
            });
        }
        catch
        {
            return Unauthorized("Podany refresh token jest zły!");
        }
    }

    //rejestracja
    [HttpPost("/auth/register")]
    public IActionResult Register(LoginRegisterRequest request)
    {
        try
        {
            //sprawdzenie czy username(email) istnieje w bazie
            var person = context.Person.FirstOrDefaultAsync(p => p.Email == request.Email);
            if (person != null)
            {
                throw new UserExistsException($"Osoba z emailem {request.Email} już istnieje");
            }
            
            //hashowanie
            var passwordHasher = new PasswordHasher<Person>();
            var hashedPassword = passwordHasher.HashPassword(new Person(), request.Password);
            
            // utworzenie obiektu
            var newPerson = new Person
            {
                Email = request.Email,
                Password = hashedPassword
            };

            // dodanie
            context.Person.Add(newPerson);
            context.SaveChangesAsync();

            return Created();
        }
        catch (Exception e)
        {
            return Conflict(e.Message);
        }
    }

    //przykladowa koncowka z zabezpieczeniem
    [HttpGet("/auth/{id:int}/info")]
    [Authorize]
    public IActionResult Info(int id)
    {
        var person = context.Person.FirstOrDefaultAsync(p => p.ID == id);
        if (person != null)
        {
            return Ok(person);
        }
        else
        {
            return NotFound($"Osoba o id {id} nie istnieje");
        }
    }

    
    public class LoginRegisterRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }
}