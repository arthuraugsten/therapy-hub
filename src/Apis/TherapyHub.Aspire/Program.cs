var builder = DistributedApplication.CreateBuilder(args);

//var sqlPassword = builder.AddParameter("sql-password", secret: true);
//var TherapyHubSql = builder.AddSqlServer("TherapyHubSql", sqlPassword, 1433);
//var patientsDb = TherapyHubSql.AddDatabase("PatientsDb");

builder.AddProject<Projects.TherapyHub_Api>("therapyhub-api");
    //.WithReference(patientsDb);

builder.Build().Run();
