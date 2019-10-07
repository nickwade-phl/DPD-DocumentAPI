﻿using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DocumentAPI.Infrastructure.Interfaces;
using DocumentAPI.Infrastructure.Models;
using System.Linq;

namespace DocumentAPI.Controllers
{
    [Route("api/v1/document-request")]
    [ApiController]
    public class DocumentRequestController : ControllerBase
    {
        private readonly IQueryAppsServices _queryAppsServices;

        public DocumentRequestController(IQueryAppsServices queryAppsServices)
        {
            _queryAppsServices = queryAppsServices;
        }


        // GET: api/v1/document-request/entities
        /// <summary>
        /// Get the initial list of Entities. Each entity has a distinct set of Categories.
        /// </summary>
        /// <returns>Json(List - Entity)</returns>
        [HttpGet("entities")]
        public JsonResult GetEntities()
        {
            return new JsonResult(DocumentCategories.Entities);
        }

        // GET: api/v1/document-request/document-categories/{entityName}
        /// <summary>
        /// Get the initial list of Document Categories. Each document category is mapped to a Repository in the ApplicationXTender.
        /// </summary>
        /// <param name="entityName">The Name property of the selected Entity</param>
        /// <returns>Json(List - Category)</returns>
        [HttpGet("document-categories/{entityName}")]
        public JsonResult GetDocumentCategories(string entityName)
        {
            return new JsonResult(DocumentCategories.Entities.SingleOrDefault(i => i.Name == entityName));
        }

        // POST: api/v1/document-request/filtered-document-list
        /// <summary>
        /// Pass a Category object with filters applied in the body of the request to retrieve a filtered list of documents and their attributes.
        /// </summary>
        /// <param name="category">A category object</param>
        /// <returns>Json(QueryAppsResult)</returns>
        [HttpPost("filtered-document-list")]
        public async Task<JsonResult> GetFilteredDocumentList([FromBody]Category category)
        {
            var xTenderDocumentList = await _queryAppsServices.FilterQueryAppsResultByParameters(category);

            if (xTenderDocumentList.Entries != null)
            {
                return new JsonResult(xTenderDocumentList.ToApiResult());
            }
            else
            {
                return new JsonResult(NotFound());
            }
        }

        // GET: api/v1/document-request/get-document/{categoryName}/{documentId}
        /// <summary>
        /// Pass in the category name and document id, and receive back the PDF represented by that id from the ApplicationXTender Repository.
        /// </summary>
        /// <param name="entityName">The Name property of the category object's Entity, which represents the Entity (Organization) which the Category belongs to</param>
        /// <param name="categoryName">The Name property of the category object, which represents the name of the Category as a repository in ApplicationXTender</param>
        /// <param name="documentId">The Id that uniquely identifies the document in the ApplicationXTender repository</param>
        /// <returns>FileStream(PDF)</returns>
        [HttpGet("get-document/{entityName}/{categoryName}/{documentId}")]
        [Produces("application/pdf", "application/problem+json")]
        public async Task<IActionResult> GetDocument(string entityName, string categoryName, int documentId)
        {
            var entity = DocumentCategories.Entities.SingleOrDefault(i => i.Name == entityName);
            var category = entity.Categories.SingleOrDefault(i => i.Name == categoryName);

            var isPublic = _queryAppsServices.CheckIfDocumentIsPublic(entity.Id, category.Id, documentId);

            if (!await isPublic)
            {
                return NotFound();
            };

            var request = _queryAppsServices.BuildDocumentRequest(entity.Id, category.Id, documentId);

            var file = await _queryAppsServices.GetResponse(request);
            return new FileStreamResult(file, "application/pdf");
        }
    }
}