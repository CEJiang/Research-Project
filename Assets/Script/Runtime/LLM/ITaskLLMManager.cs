using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ITaskLLMManager
{
    Task<string> RunTask(
        string userInput,
        JsonSchemaFormat format = default
    );
}
