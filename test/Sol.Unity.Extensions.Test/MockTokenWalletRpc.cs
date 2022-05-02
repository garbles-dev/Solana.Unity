﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Sol.Unity.Rpc.Core.Http;
using Sol.Unity.Rpc.Messages;
using Sol.Unity.Rpc.Models;
using Sol.Unity.Rpc.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Sol.Unity.Extensions.Test
{
    internal class MockTokenWalletRpc : ITokenWalletRpcProxy
    {

        /// <summary>
        /// Queue of RPC responses
        /// </summary>
        private Queue<string> _responses;

        /// <summary>
        /// The request sequence number
        /// </summary>
        private int _reqSeqId;

        /// <summary>
        /// The Json serializer options to be reused between calls.
        /// </summary>
        private readonly JsonSerializerSettings _serializerOptions;

        /// <summary>
        /// Constructor for the MockWalletRpc
        /// </summary>
        public MockTokenWalletRpc()
        {
            _reqSeqId = 0;
            _responses = new Queue<string>();
            _serializerOptions = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters =
                {
                    new StringEnumConverter(new CamelCaseNamingStrategy())
                }
            };
        }


        public void AddTextFile(string filepath)
        {
            _responses.Enqueue(File.ReadAllText(filepath));
        }


        private async Task<RequestResult<ResponseValue<T>>> MockResponseValue<T>()
        {
            JsonRpcResponse<ResponseValue<T>> result = GetNextJsonResponse<ResponseValue<T>>();

            // package as a successful response
            var reqResult = new RequestResult<ResponseValue<T>>();
            reqResult.WasHttpRequestSuccessful = true;
            reqResult.WasRequestSuccessfullyHandled = true;
            reqResult.HttpStatusCode = HttpStatusCode.OK;
            reqResult.Result = result.Result;
            return await Task.FromResult<RequestResult<ResponseValue<T>>>(reqResult);

        }

        private async Task<RequestResult<T>> MockValue<T>()
        {
            JsonRpcResponse<T> result = GetNextJsonResponse<T>();

            // package as a successful response
            var reqResult = new RequestResult<T>();
            reqResult.WasHttpRequestSuccessful = true;
            reqResult.WasRequestSuccessfullyHandled = true;
            reqResult.HttpStatusCode = HttpStatusCode.OK;
            reqResult.Result = result.Result;
            return await Task.FromResult<RequestResult<T>>(reqResult);

        }

        private JsonRpcResponse<T> GetNextJsonResponse<T>()
        {
            // make sure we've got a queued response 
            if (_responses.Count == 0) throw new ApplicationException("Mock responses exhausted");

            // thread-safe increment response id + dequeue
            int id = 0;
            string json = null;
            lock (this)
            {
                _reqSeqId++;
                id = _reqSeqId;
                json = _responses.Dequeue();
            }

            // deserialize JSON RPC response
            var result = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(json, _serializerOptions);
            if (result == null) throw new ApplicationException("Mock response did not deserialize");

            // overwrite response id
            result.Id = id;
            return result;
        }

        public async Task<RequestResult<ResponseValue<ulong>>> GetBalanceAsync(string pubKey, Commitment commitment = Commitment.Finalized)
        {
            return await MockResponseValue<ulong>();
        }

        public async Task<RequestResult<ResponseValue<BlockHash>>> GetRecentBlockHashAsync(Commitment commitment = Commitment.Finalized)
        {
            return await MockResponseValue<BlockHash>();
        }

        public async Task<RequestResult<ResponseValue<List<TokenAccount>>>> GetTokenAccountsByOwnerAsync(string ownerPubKey, string tokenMintPubKey = null, string tokenProgramId = null, Commitment commitment = Commitment.Finalized)
        {
            return await MockResponseValue<List<TokenAccount>>();
        }

        public async Task<RequestResult<string>> SendTransactionAsync(byte[] transaction, bool skipPreflight = false, Commitment commitment = Commitment.Finalized)
        {
            return await MockValue<string>();
        }
    }
}
