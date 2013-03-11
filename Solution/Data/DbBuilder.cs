using System;
using System.IO;
using Fabric.Apps.WordNet.Data.Mapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace Fabric.Apps.WordNet.Data {

	/*================================================================================================*/
	public static class DbBuilder {

		public const string DbFile = "FabWordNet.db";
		
		public static ISessionFactory SessionFactory { get; private set; }

		private static Configuration Config;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void InitOnce() {
			if ( SessionFactory != null ) {
				throw new Exception("Factory has already been initialized.");
			}

			string root = Directory.GetCurrentDirectory();
			string path = Path.GetFullPath(root+"/../../../../Data/"+DbFile);
			Console.WriteLine("Conn: "+"Data Source="+path+";Version=3");

			IPersistenceConfigurer conn = SQLiteConfiguration
				.Standard
				.ConnectionString("Data Source="+path+";Version=3");

			SessionFactory = Fluently.Configure()
				.Database(conn)
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<ArtifactMap>())
				.ExposeConfiguration(c => { Config = c; })
				.BuildSessionFactory();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public static void UpdateSchema() {
			var schema = new SchemaUpdate(Config);
			schema.Execute(true, true);
		}

		/*--------------------------------------------------------------------------------------------*/
		public static void EraseAndRebuildDatabase() {
			if ( File.Exists(DbFile) ) {
				File.Delete(DbFile);
			}

			var schema = new SchemaExport(Config);
			schema.Create(false, true);
		}

	}

}