using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SaqayaProject.Data;
using SaqayaProject.Dto;
using SaqayaProject.models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SaqayaProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DataContext _dbContext;
        private readonly IConfiguration _configuration;

        public UserController(DataContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserDTO userDTO)
        {
         
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Generate id (SHA1 hash of email with salt)
            string id = GenerateUserId(userDTO.email);

            // Generate accessToken (JWT Token)
            string accessToken = GenerateAccessToken(id);

            // Create User entity
            var user = new user
            {
                Id = id,
                firstName = userDTO.firstName,
                lastName = userDTO.lastName,
                email = userDTO.email,
                marketingConsent = userDTO.marketingConsent
            };

           
            _dbContext.users.Add(user);
            _dbContext.SaveChanges();


            return Ok(new { Id = id, AccessToken = accessToken });
        }

        
        [HttpGet("{id}")]
        public IActionResult GetUser(string id, [FromQuery] string accessToken)
        {
            
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(accessToken))
            {
                return BadRequest("Id and accessToken are required.");
            }

            // Validate accessToken
            if (!ValidateAccessToken(accessToken, id))
            {
                return Unauthorized("Invalid accessToken.");
            }

            // Fetch the user from the database
            var user = _dbContext.users.Find(id);

            // Check if the user exists
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // If marketing consent is false, omit the email property
            if (!user.marketingConsent)
            {
                return Ok(new UserDTO
                {
                    Id = user.Id,
                    firstName = user.firstName,
                    lastName = user.lastName,
                    marketingConsent = user.marketingConsent
                });
            }

            // Return the user with full details
            return Ok(new UserDTO
            {
                Id = user.Id,
                firstName = user.firstName,
                lastName = user.lastName,
                email = user.email,
                marketingConsent = user.marketingConsent
            });
        }
      

        //  to generate SHA1 hash of email with salt
        private string GenerateUserId(string email)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(email + "450d0b0db2bcf4adde5032eca1a7c416e560cf44"));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // to generate JWT Token
        
        private string GenerateAccessToken(string userId)
        {
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("Jwt:Secret").Value);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim(ClaimTypes.Name, userId)
                }),
                Expires = DateTime.Now.AddHours(3), // Token expiration time
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }





        // to validate JWT Token
       
        private bool ValidateAccessToken(string token, string userId)
        {
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("Jwt:Secret").Value) ;

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;

                return userIdClaim == userId;
            }
            catch
            {
                return false;
            }
        }
     
       


    }
}

