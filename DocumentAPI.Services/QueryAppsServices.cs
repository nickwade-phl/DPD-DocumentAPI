﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DocumentAPI.Infrastructure.Interfaces;
using DocumentAPI.Infrastructure.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace DocumentAPI.Services
{
    public class QueryAppsServices : IQueryAppsServices
    {
        private readonly HttpClient _httpClient;
        private readonly Config _config;
        private ILogger _logger;
        private readonly DepartmentEntities _departmentEntities;
        private static IAmazonS3 _s3Client;
        private readonly string _awsBucket;
        private static RegionEndpoint _awsRegion;

        public QueryAppsServices(HttpClient httpClient, IConfiguration config, ILogger<QueryAppsServices> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config.Load();

            _awsBucket = _config.S3BucketName;
            _awsRegion = RegionEndpoint.GetBySystemName(_config.S3Region);
            var credentials = new BasicAWSCredentials(_config.S3AccessKeyID, _config.S3SecretAccessKey);
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = _awsRegion
            };

            _s3Client = new AmazonS3Client(credentials, s3Config);

            _departmentEntities = new DepartmentEntities(config);
        }


        public async Task<ICollection<Entity>> GetEntities()
        {
            return _departmentEntities.Entities;
        }

        public async Task<QueryAppsResult> FilterQueryAppsResultByParameters(Category category)
        {
            var filteredAttributes = category.Attributes.Where(i => i.SelectedFilterType != null);
            var adhocQueryObject = new QueryRequestObject();

            foreach (var filteredAttribute in filteredAttributes)
            {
                var query = "";
                if (filteredAttribute.Type.Name == DepartmentEntities.TextTypeName)
                {
                    var stringFilter1 = filteredAttribute.FilterValue1;
                    var stringFilter2 = filteredAttribute.FilterValue2;

                    switch (filteredAttribute.SelectedFilterType.Name)
                    {
                        case DepartmentEntities.TextEqualsOperator:
                            query = $"{stringFilter1}";
                            break;
                        case DepartmentEntities.TextGreaterThanOperator:
                            query = $"Expression: > {stringFilter1}";
                            break;
                        case DepartmentEntities.TextLessThanOperator:
                            query = $"Expression: < {stringFilter1}";
                            break;
                        case DepartmentEntities.TextBetweenOperator:
                            query = $"Expression: ['{stringFilter1}','{stringFilter2}']";
                            break;
                    }
                }
                else if (filteredAttribute.Type.Name == DepartmentEntities.DateTypeName)
                {
                    var dateFilter1 = (DateTime.TryParse(filteredAttribute.FilterValue1, out var date1Value) ? date1Value : DateTime.MinValue).ToShortDateString();
                    var dateFilter2 = (DateTime.TryParse(filteredAttribute.FilterValue2, out var date2Value) ? date2Value : DateTime.MinValue).ToShortDateString();

                    switch (filteredAttribute.SelectedFilterType.Name)
                    {
                        case DepartmentEntities.DateEqualsOperator:
                            query = $"{dateFilter1}";
                            break;
                        case DepartmentEntities.DateGreaterThanOperator:
                            query = $"Expression: > {dateFilter1}";
                            break;
                        case DepartmentEntities.DateLessThanOperator:
                            query = $"Expression: < {dateFilter1}";
                            break;
                        case DepartmentEntities.DateBetweenOperator:
                            query = $"Expression: ['{dateFilter1}','{dateFilter2}']";
                            break;
                    }
                }
                else if (filteredAttribute.Type.Name == DepartmentEntities.NumericTypeName)
                {
                    var numericFilter1 = int.TryParse(filteredAttribute.FilterValue1, out var int1Value) ? int1Value : -1;
                    var numericFilter2 = int.TryParse(filteredAttribute.FilterValue2, out var int2Value) ? int2Value : -1;

                    switch (filteredAttribute.SelectedFilterType.Name)
                    {
                        case DepartmentEntities.NumericEqualsOperator:
                            query = $"{numericFilter1}";
                            break;
                        case DepartmentEntities.NumericGreaterThanOperator:
                            query = $"Expression: > {numericFilter1}";
                            break;
                        case DepartmentEntities.NumericLessThanOperator:
                            query = $"Expression: < {numericFilter1}";
                            break;
                        case DepartmentEntities.NumericBetweenOperator:
                            query = $"Expression: ['{numericFilter1}','{numericFilter2}']";
                            break;
                    }
                }
                else if (filteredAttribute.Type.Name == DepartmentEntities.FullTextSearchName)
                {
                    query = filteredAttribute.FilterValue1;
                }

                if (!string.IsNullOrEmpty(query))
                {
                    if (filteredAttribute.Type.Name == DepartmentEntities.FullTextSearchName)
                    {
                        adhocQueryObject.FullText = new FullText(query);
                    }
                    else
                    {
                        adhocQueryObject.Indexes.Add(new Infrastructure.Models.Index(filteredAttribute.Name, query));
                    }
                }
            }

            var xTenderDocumentList = await RequestDocuments(category.EntityId.GetValueOrDefault(), category.Id.GetValueOrDefault(), adhocQueryObject, true);

            return xTenderDocumentList;
        }

        public async Task<QueryAppsResult> GetAllDocuments(int entityId, int categoryId)
        {
            var documentList = RequestDocuments(entityId, categoryId);
            var docsWithPageCounts = RequestDocumentsFromOracle(entityId, categoryId);

            foreach (var document in (await documentList).Entries)
            {
                document.PageCount = (await docsWithPageCounts)?.Entries.SingleOrDefault(i => i.Id == document.Id)?.PageCount ?? 0;
            }
            return await documentList;
        }

        private async Task<QueryAppsResult> RequestDocuments(int entityId, int categoryId, QueryRequestObject queryRequest = null, bool isAdHocQueryRequest = false)
        {
            var entities = await GetEntities();
            var entity = entities.SingleOrDefault(i => i.Id == entityId);
            var category = entity.Categories.SingleOrDefault(i => i.Id == categoryId);

            var requestPath = isAdHocQueryRequest ? _config.AdHocQueryResultsPath : _config.SelectIndexLookupPath;
            var query = new UriBuilder($"{requestPath}/{category.Id}");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _config.Credentials);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.emc.ax+json"));

            var queryJson = JsonConvert.SerializeObject(new string[0]);

            if (isAdHocQueryRequest)
            {
                queryJson = JsonConvert.SerializeObject(queryRequest);
            }
            using (var request = await _httpClient.PostAsync(query.Uri, new StringContent(queryJson, Encoding.UTF8, "application/vnd.emc.ax+json")))
            {
                var result = JsonConvert.DeserializeObject<QueryAppsResult>(await request.Content.ReadAsStringAsync());
                var filteredResult = result.Entries != null && result.Entries.Any() ? ExcludeNonPublicDocuments(result, category) : result;
                return filteredResult;
            }
        }

        private async Task<QueryAppsResult> RequestDocumentsFromOracle(int entityId, int categoryId)
        {
            var entities = await GetEntities();
            var entity = entities.SingleOrDefault(i => i.Id == entityId);
            var category = entity.Categories.SingleOrDefault(i => i.Id == categoryId);
            var result = new QueryAppsResult();
            var oracleConnectionString = _config.OracleConnectionString;
            using (var axOracleDb = new OracleConnection(oracleConnectionString))
            {
                using (OracleCommand cmd = axOracleDb.CreateCommand())
                {
                    try
                    {
                        await axOracleDb.OpenAsync();
                        var resultDataSet = new DataSet();
                        cmd.CommandText = $"select DOCID, PAGES from historical where APPID = {category.Id}";

                        using (var dataAdapter = new OracleDataAdapter())
                        {
                            dataAdapter.SelectCommand = cmd;
                            dataAdapter.Fill(resultDataSet);
                        }

                        var table = resultDataSet.Tables[0];

                        if (table != null)
                        {
                            foreach (DataRow row in table.Rows)
                            {

                                var entry = new Entry
                                {
                                    Id = int.TryParse(row.ItemArray.First().ToString(), out var id) ? id : 0,
                                    PageCount = int.TryParse(row.ItemArray.Last().ToString(), out var pageCount) ? pageCount : 0
                                };
                                result.Entries.Add(entry);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }
            return result;
        }

        private QueryAppsResult ExcludeNonPublicDocuments(QueryAppsResult result, Category category)
        {
            var columnIndex = Array.FindIndex(result.Columns, i => i == category.NotPublicFieldName);

            if (columnIndex > -1)
            {
                result.Entries = result.Entries.Where(i =>
                    // parse and filter out entries where Not Public equals true
                    // default to not public for documents that do not have a value
                    !(bool.TryParse(i.IndexValues[columnIndex], out var notPublic) ? notPublic : true)
                ).ToList();
            };
            return result;
        }

        public async Task<HttpRequestMessage> BuildDocumentRequest(int entityId, int categoryId, int documentId)
        {
            var requestUrlString = "";
            var awsObjectKey = $"{categoryId}-{documentId}.pdf";
            try
            {
                var requestUrl = new GetPreSignedUrlRequest
                {
                    BucketName = _awsBucket,
                    Key = awsObjectKey,
                    Expires = DateTime.Now.AddMinutes(2)
                };
                requestUrlString = _s3Client.GetPreSignedURL(requestUrl);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("AWS error encountered on server. Message:'{0}' when generating a presigned request URL", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown error encountered on server. Message:'{0}' when generating a presigned request URL", e.Message);
            }

            var requestMessage = new HttpRequestMessage();
            var getFile = new UriBuilder(requestUrlString);
            requestMessage.RequestUri = getFile.Uri;
            requestMessage.Method = HttpMethod.Get;

            _logger.LogInformation($"Request built to get document from {requestMessage.RequestUri}");

            return requestMessage;
        }

        public async Task<bool> CheckIfDocumentIsPublic(int entityId, int categoryId, int documentId)
        {
            var entities = await GetEntities();
            var entity = entities.SingleOrDefault(i => i.Id == entityId);
            var category = entity.Categories.SingleOrDefault(i => i.Id == categoryId);

            var adhocQueryRequest = new QueryRequestObject();
            var isPublicDocument = false;

            if (category != null)
            {
                if (!string.IsNullOrEmpty(category.NotPublicFieldName))
                {
                    adhocQueryRequest.Indexes.Add(new Infrastructure.Models.Index(category.NotPublicFieldName, "FALSE"));

                    var adhocQueryJson = JsonConvert.SerializeObject(adhocQueryRequest.Indexes);

                    var query = new UriBuilder($"{_config.SelectIndexLookupPath}/{category.Id}");

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _config.Credentials);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.emc.ax+json"));

                    using (var request = await _httpClient.PostAsync(query.Uri, new StringContent(adhocQueryJson, Encoding.UTF8, "application/vnd.emc.ax+json")))
                    {
                        var result = JsonConvert.DeserializeObject<QueryAppsResult>(await request.Content.ReadAsStringAsync());
                        var requestedRecord = result.Entries.SingleOrDefault(i => i.Id == documentId);
                        if (requestedRecord != null)
                        {
                            isPublicDocument = true;
                        }
                    }
                }
                else
                {
                    // no field signifying non-public document? then all documents are public
                    isPublicDocument = true;
                }
            }
            return isPublicDocument;
        }

        public async Task<Stream> GetResponse(HttpRequestMessage requestMessage)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            var response = await _httpClient.SendAsync(requestMessage);
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
