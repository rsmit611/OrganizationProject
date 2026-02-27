Here's my current vision of the project layout:  
Asssets: Any assets we need to use will go in here  
Source/OrganizationProject: WPF UI (Windows, viewing, etc)  
Source/OrganizationProject.Core: Core Logic (The backbone of the program)  
tests: Test files  


I've made a simple note object and a test for the note, so that I could confirm that "dotnet build" and "dotnet test" were working properly.

To get everything working on your computer, make sure you install git, Dotnet SDK 9, and an IDE of your choice (I recommend visual studio).

Find a place to put the folder, and open command prompt at that folder and type "git clone https://github.com/rsmit611/OrganizationProject.git"  
(You can open command prompt at any folder in explorer by clicking the file path at the top and just typing cmd)  
This will create a new folder and inside will be the repository.

You can check if everything is working by running "dotnet build" and "dotnet test" at the root of the project. Alternatively you can use visual studio to build and test the solution.
