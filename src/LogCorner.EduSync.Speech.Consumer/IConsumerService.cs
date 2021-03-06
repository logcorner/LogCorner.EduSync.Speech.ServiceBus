﻿using System.Threading;
using System.Threading.Tasks;

namespace LogCorner.EduSync.Speech.Consumer
{
    public interface IConsumerService
    {
        Task DoWorkAsync(CancellationToken stoppingToken);
    }
}