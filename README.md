##Database

dotnet ef migrations add --project App.DAL.EF --startup-project Exam --context ApplicationDbContext Initial

dotnet ef migrations remove --project App.DAL.EF --startup-project Exam --context ApplicationDbContext

dotnet ef database update --project App.DAL.EF --startup-project Exam

dotnet ef database drop --project App.DAL.EF --startup-project Exam


cd Exam

##Controllers

dotnet aspnet-codegenerator controller -name ItemController       -actions -m  App.Domain.Item    -dc ApplicationDbContext -outDir Areas\Admin\Controllers --useDefaultLayout --useAsyncActions --referenceScriptLibraries -f

##Api controllers

dotnet aspnet-codegenerator controller -name ItemController     -m App.Domain.Item     -actions -dc App.DAL.EF.ApplicationDbContext -outDir ApiControllers -api --useAsyncActions  -f
