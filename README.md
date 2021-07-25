# JiraWorklogs2019

This is the beginning of a Visual Studio plugin to manage Jira worklog timers within Visual Studio.

Given Atlassian's move to force people to the cloud along with their continual push to lock things tighter and tighter this may or may not be of use in your particular situation.
Please note the following:

This plugin uses basic username/password authentication for access. If your Jira instance uses OAuth, I don't have a way to test that so I cannot support that.
An API key may be used in place of the password. To do so you would just enter your API key in the password box.
If your Jira is set up for MFA this plugin may or may not work.

Those are the issues that I know about at this point.

Usage is rather simple.

Go to Tools->Options after installation and look for the Jira Worklogs entry. Fill in the server URI and your username. Then go to View->Other Windows->Jira Worklogs.
Then enter your password/API key and click Retrieve.

Note that the plugin will only retrieve issues where you have already set up original/estimated time, etc.
