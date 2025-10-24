using MiniETicaret.Products.WebAPI.Models;

namespace MiniETicaret.Products.WebAPI.Dtos
{
   
        public sealed class Result<T>
        {
            public Result()
            {

            }
            public Result(T data)
            {
                Data = data;
            }
            public T? Data { get; set; } = default;

      
    }
    }

