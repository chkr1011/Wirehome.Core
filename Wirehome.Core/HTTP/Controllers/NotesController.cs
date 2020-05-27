using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class NotesController : Controller
    {
        private const string NotesDirectory = "Notes";

        private readonly StorageService _storageService;

        public NotesController(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        [HttpGet]
        [Route("api/v1/notes/uids")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetNoteUids()
        {
            return _storageService.EnumerateFiles("*.md", NotesDirectory).Select(f => f.Substring(0, f.Length - 3)).ToList();
        }

        [HttpGet]
        [Route("api/v1/notes/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public string GetNote(string uid)
        {
            var filename = $"{uid}.md";

            if (!_storageService.TryReadRawText(out var note, NotesDirectory, filename))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return note;
        }

        [HttpPost]
        [Route("api/v1/notes/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostNote(string uid, [FromBody] string text)
        {
            var filename = $"{uid}.md";
            _storageService.WriteRawText(text, NotesDirectory, filename);
        }

        [HttpDelete]
        [Route("api/v1/notes/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteNote(string uid)
        {
            var filename = $"{uid}.md";
            _storageService.DeletePath(NotesDirectory, filename);
        }
    }
}
