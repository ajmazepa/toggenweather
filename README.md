![](https://github.com/ajmazepa/toggenweather/blob/main/img/toggenweather.jpg)

# toggenweather
A quick experiment to cut out the noise (ads) and get a quick snapshot of today's weather forecast via email and SMS. ** PLEASE NOTE ** This is a quick proof of concept and does not follow best practices. Credentials should be stored securely and not in the App.config file. This program uses the Selenium driver to read and summarize an hourly Weather Network forecast and send the results via email and SMS using Amazon Simple Notification Service.

## Installation
This project is for a console application. Add the App.config and Global and Program classes to your project. Update the App.config file for your specific forecast, email, and Amazon SNS settings.

## Usage
This console application can be run on a server using a scheduled task to send regular weather forecast notifications.