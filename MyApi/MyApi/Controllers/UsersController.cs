using Microsoft.AspNetCore.Mvc;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<User>>> GetAll(CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetAllAsync(cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetByIdAsync(id, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _userService.CreateAsync(request, cancellationToken);
            if (user is null)
            {
                return Conflict(new { message = "邮箱已存在" });
            }

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<User>> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var (result, user) = await _userService.UpdateAsync(id, request, cancellationToken);
            return result switch
            {
                UserWriteResult.NotFound => NotFound(),
                UserWriteResult.EmailConflict => Conflict(new { message = "邮箱已存在" }),
                _ => Ok(user)
            };
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            return await _userService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
    }
}
