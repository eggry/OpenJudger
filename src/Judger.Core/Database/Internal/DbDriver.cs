﻿using System;
using System.Data.Common;
using System.Reflection;

namespace Judger.Core.Database.Internal
{
    /// <summary>
    /// 数据库驱动
    /// </summary>
    public class DbDriver
    {
        /// <summary>
        /// DbConnection类
        /// </summary>
        public Type DbConnectionType { get; private set; }

        /// <summary>
        /// DbCommand类
        /// </summary>
        public Type DbCommandType { get; private set; }

        /// <summary>
        /// DbDriver程序集
        /// </summary>
        public Assembly DriverAssembly { get; private set; }

        /// <summary>
        /// 数据库驱动
        /// </summary>
        /// <param name="dbConnection">DbConnection类</param>
        /// <param name="dbCommand">DbCommand类</param>
        /// <param name="assembly">数据库驱动程序集</param>
        public DbDriver(Type dbConnection, Type dbCommand, Assembly assembly = null)
        {
            DbConnectionType = dbConnection;
            DbCommandType = dbCommand;
            DriverAssembly = assembly ?? DbConnectionType.Assembly;
        }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="openConnection">创建后是否打开连接</param>
        /// <returns></returns>
        public DbConnection CreateConnection(string connectionString, bool openConnection = true)
        {
            object[] args = new object[] {connectionString};
            DbConnection conn = CreateInstance(DbConnectionType.FullName, args) as DbConnection;
            if (conn != null && openConnection)
            {
                conn.Open();
            }

            return conn;
        }

        /// <summary>
        /// 创建数据库命令
        /// </summary>
        /// <param name="cmdText">命令</param>
        /// <param name="connection">数据库连接</param>
        /// <returns></returns>
        public DbCommand CreateCommand(string cmdText, DbConnection connection)
        {
            object[] args = new object[] {cmdText, connection};
            DbCommand command = CreateInstance(DbCommandType.FullName, args) as DbCommand;

            return command;
        }

        private object CreateInstance(string fullName, object[] args)
        {
            return DbCommandType.Assembly.CreateInstance(fullName, true, BindingFlags.Default, null, args, null, null);
        }
    }
}