using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServiceControl.Domain.Intefaces;

interface IWeatherService
{
    async getWeatherData(city, date)
    {
        throw new Error('Method must be implemented');
    }
}

class IRepository
{
    async save(entity)
    {
        throw new Error('Method must be implemented');
    }

    async findAll()
    {
        throw new Error('Method must be implemented');
    }
}

class IMessageBroker
{
    async sendMessage(destination, message)
    {
        throw new Error('Method must be implemented');
    }
}

class IRetryStrategy
{
    async execute(operation, context)
    {
        throw new Error('Method must be implemented');
    }
}