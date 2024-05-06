using Blog.Core.Common;
using Blog.Core.Common.DB;
using Blog.Core.IRepository.Base;
using Blog.Core.Model;
using Blog.Core.Model.Models;
using Blog.Core.Model.Tenants;
using Blog.Core.Repository.UnitOfWorks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Blog.Core.Repository.Base
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, new()
    {
        private readonly IUnitOfWorkManage _unitOfWorkManage;
        private readonly SqlSugarScope _dbBase;

        private ISqlSugarClient _db
        {
            get
            {
                ISqlSugarClient db = _dbBase;

                //修改使用 model备注字段作为切换数据库条件，使用sqlsugar TenantAttribute存放数据库ConnId
                //参考 https://www.donet5.com/Home/Doc?typeId=2246
                var tenantAttr = typeof(TEntity).GetCustomAttribute<TenantAttribute>();
                if (tenantAttr != null)
                {
                    //统一处理 configId 小写
                    db = _dbBase.GetConnectionScope(tenantAttr.configId.ToString().ToLower());
                    return db;
                }

                //多租户
                var mta = typeof(TEntity).GetCustomAttribute<MultiTenantAttribute>();
                if (mta is { TenantType: TenantTypeEnum.Db })
                {
                    //获取租户信息 租户信息可以提前缓存下来 
                    if (App.User is { TenantId: > 0 })
                    {
                        var tenant = db.Queryable<SysTenant>().WithCache().Where(s => s.Id == App.User.TenantId).First();
                        if (tenant != null)
                        {
                            var iTenant = db.AsTenant();
                            if (!iTenant.IsAnyConnection(tenant.ConfigId))
                            {
                                iTenant.AddConnection(tenant.GetConnectionConfig());
                            }

                            return iTenant.GetConnectionScope(tenant.ConfigId);
                        }
                    }
                }

                return db;
            }
        }

        public ISqlSugarClient Db => _db;

        public BaseRepository(IUnitOfWorkManage unitOfWorkManage)
        {
            _unitOfWorkManage = unitOfWorkManage;
            _dbBase = unitOfWorkManage.GetDbClient();
        }

        #region 根据ID来查询（不适用于联合主键
        /// <summary>
        /// 功能描述:根据ID查询一条数据
        /// </summary>
        /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键（即，不只一个字段作为主键），请使用Where条件</param>
        /// <returns></returns>
        public async Task<TEntity> QueryById(object objId)
        {
            //return await Task.Run(() => _db.Queryable<TEntity>().InSingle(objId));
            return await _db.Queryable<TEntity>().In(objId).SingleAsync();
        }

        /// <summary>
        /// 功能描述:根据ID查询一条数据
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="objId">id（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
        /// <param name="blnUseCache">是否使用缓存</param>
        /// <returns>数据实体</returns>
        public async Task<TEntity> QueryById(object objId, bool blnUseCache = false)
        {
            //return await Task.Run(() => _db.Queryable<TEntity>().WithCacheIF(blnUseCache).InSingle(objId));
            return await _db.Queryable<TEntity>().WithCacheIF(blnUseCache, 10).In(objId).SingleAsync(); //启用缓存，缓存时间为10分钟
        }

        /// <summary>
        /// 功能描述:根据ID查询数据
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="lstIds">id列表（必须指定主键特性 [SugarColumn(IsPrimaryKey=true)]），如果是联合主键，请使用Where条件</param>
        /// <returns>数据实体列表</returns>
        public async Task<List<TEntity>> QueryByIDs(object[] lstIds)
        {
            //return await Task.Run(() => _db.Queryable<TEntity>().In(lstIds).ToList());
            return await _db.Queryable<TEntity>().In(lstIds).ToListAsync();
        }
        #endregion

        #region 插入
        /// <summary>
        /// 写入实体数据
        /// </summary>
        /// <param name="entity">博文实体类</param>
        /// <returns></returns>
        public async Task<long> Add(TEntity entity)
        {
            //var i = await Task.Run(() => _db.Insertable(entity).ExecuteReturnBigIdentity());
            ////返回的i是long类型,这里你可以根据你的业务需要进行处理
            //return (int)i;

            var insert = _db.Insertable(entity);

            //这里你可以返回TEntity，这样的话就可以获取id值，无论主键是什么类型
            //var return3 = await insert.ExecuteReturnEntityAsync();

            return await insert.ExecuteReturnSnowflakeIdAsync();
        }

        /// <summary>
        /// 写入实体数据
        /// </summary>
        /// <param name="entity">实体类</param>
        /// <param name="insertColumns">指定只插入列</param>
        /// <returns>返回自增量列</returns>
        public async Task<long> Add(TEntity entity, Expression<Func<TEntity, object>> insertColumns = null)
        {
            var insert = _db.Insertable(entity);
            if (insertColumns == null)
            {
                return await insert.ExecuteReturnSnowflakeIdAsync();
            }
            /*  在这个 Add 方法中，如果 insertColumns 参数为 null，那么插入操作会包含所有的列。
                如果 insertColumns 参数不为 null，那么插入操作只会包含 insertColumns 指定的列。
                举个例子，假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们只想插入 Name 和 Age，可以这样使用 Add 方法：
                var person = new Person { Id = 1, Name = "Tom", Age = 20 };
                await Add(person, p => new { p.Name, p.Age });*/
            else
            {
                return await insert.InsertColumns(insertColumns).ExecuteReturnSnowflakeIdAsync();
            }
        }

        /// <summary>
        /// 批量插入实体(速度快)
        /// </summary>
        /// <param name="listEntity">实体集合</param>
        /// <returns>影响行数</returns>
        public async Task<List<long>> Add(List<TEntity> listEntity)
        {
            return await _db.Insertable(listEntity.ToArray()).ExecuteReturnSnowflakeIdListAsync();
        }
        #endregion

        #region 更新
        /// <summary>
        /// 更新实体数据，默认以主键为条件
        /// </summary>
        /// <param name="entity">博文实体类</param>
        /// <returns></returns>
        public async Task<bool> Update(TEntity entity)
        {
            ////这种方式会以主键为条件
            //var i = await Task.Run(() => _db.Updateable(entity).ExecuteCommand());
            //return i > 0;
            //这种方式会以主键为条件
            return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
        }
        /// <summary>
        /// 批量更新实体数据，默认都以主键为条件
        /// </summary>
        /// <param name="entity">博文实体类</param>
        /// <returns></returns>
        public async Task<bool> Update(List<TEntity> entity)
        {
            return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
        }

        /// <summary>
        /// 更新实体数据，以where为条件
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="where">条件</param>
        /// <returns></returns>
        public async Task<bool> Update(TEntity entity, string where)
        {
            /*假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们想要更新 Id 为 1 的记录的 Name 和 Age，可以这样使用 Update 方法：
var person = new Person { Id = 1, Name = "Tom", Age = 20 };
await Update(person, "Id = 1");
在这个例子中，entity 参数是一个 Person 对象，where 参数是一个字符串 "Id = 1"，表示我们想要更新 Id 为 1 的记录。

这个方法会返回一个 bool 值，表示更新操作是否成功。如果更新操作影响了至少一条记录，那么这个方法会返回 true；否则，返回 false。*/
            return await _db.Updateable(entity).Where(where).ExecuteCommandHasChangeAsync();
        }
        /// <summary>
        /// 更新实体数据，原生sql
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<bool> Update(string sql, SugarParameter[] parameters = null)
        {
            /*假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们想要更新 Id 为 1 的记录的 Name 和 Age，可以这样使用 Update 方法：
string sql = "UPDATE Person SET Name = @name, Age = @age WHERE Id = @id";
SugarParameter[] parameters = new SugarParameter[]
{
    new SugarParameter("@name", "Tom"),
    new SugarParameter("@age", 20),
    new SugarParameter("@id", 1)
};
await Update(sql, parameters);
在这个例子中，sql 参数是一个 SQL 更新语句，parameters 参数是一个 SugarParameter 数组，包含 SQL 语句中的参数。*/
            return await _db.Ado.ExecuteCommandAsync(sql, parameters) > 0;
        }

        /// <summary>
        /// 更新实体数据，匿名，以主键为条件
        /// </summary>
        /// <param name="operateAnonymousObjects"></param>
        /// <returns></returns>
        public async Task<bool> Update(object operateAnonymousObjects)
        {
            /*假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们想要更新 Id 为 1 的记录的 Name 和 Age，可以这样使用 Update 方法：
var updateObject = new { Id = 1, Name = "Tom", Age = 20 };
await Update(updateObject);
在这个例子中，operateAnonymousObjects 参数是一个匿名对象，包含 Person 类的 Id、Name 和 Age 属性。
这个方法会根据 Id 属性找到对应的记录，然后更新 Name 和 Age 这两列。*/
            return await _db.Updateable<TEntity>(operateAnonymousObjects).ExecuteCommandAsync() > 0;
        }

        public async Task<bool> Update(
            TEntity entity,
            List<string> lstColumns = null,
            List<string> lstIgnoreColumns = null,
            string where = ""
        )
        /*它接受四个参数：一个是要更新的实体 entity，一个是要更新的列的列表 lstColumns，一个是要忽略的列的列表 lstIgnoreColumns，
          还有一个是用于筛选要更新的记录的条件 where。

假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们想要更新 Id 为 1 的记录的 Name 和 Age，并且忽略 Age 列，可以这样使用 Update 方法：
var person = new Person { Id = 1, Name = "Tom", Age = 20 };
List<string> lstColumns = new List<string> { "Name", "Age" };
List<string> lstIgnoreColumns = new List<string> { "Age" };
string where = "Id = 1";
await Update(person, lstColumns, lstIgnoreColumns, where);
在这个例子中，entity 参数是一个 Person 对象，lstColumns 参数是一个包含 Name 和 Age 的列表，
lstIgnoreColumns 参数是一个包含 Age 的列表，where 参数是一个字符串 "Id = 1"。*/
        {
            IUpdateable<TEntity> up = _db.Updateable(entity);
            if (lstIgnoreColumns != null && lstIgnoreColumns.Count > 0)
            {
                up = up.IgnoreColumns(lstIgnoreColumns.ToArray());
            }

            if (lstColumns != null && lstColumns.Count > 0)
            {
                up = up.UpdateColumns(lstColumns.ToArray());
            }

            if (!string.IsNullOrEmpty(where))
            {
                up = up.Where(where);
            }

            return await up.ExecuteCommandHasChangeAsync();
        }
        #endregion

        #region 删除
        /// <summary>
        /// 根据实体删除一条数据
        /// </summary>
        /// <param name="entity">博文实体类</param>
        /// <returns></returns>
        public async Task<bool> Delete(TEntity entity)
        {
            return await _db.Deleteable(entity).ExecuteCommandHasChangeAsync();
        }

        /// <summary>
        /// 删除指定ID的数据
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <returns></returns>
        public async Task<bool> DeleteById(object id)
        {
            return await _db.Deleteable<TEntity>().In(id).ExecuteCommandHasChangeAsync();
        }

        /// <summary>
        /// 删除指定ID集合的数据(批量删除)
        /// </summary>
        /// <param name="ids">主键ID集合</param>
        /// <returns></returns>
        public async Task<bool> DeleteByIds(object[] ids)
        {
            return await _db.Deleteable<TEntity>().In(ids).ExecuteCommandHasChangeAsync();
        }
        #endregion

        #region 简单查询
        /// <summary>
        /// 功能描述:查询所有数据
        /// 作　　者:Blog.Core
        /// </summary>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query()
        {
            return await _db.Queryable<TEntity>().ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询数据列表
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="where">条件</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(string where) // 原生条件
        {
            return await _db.Queryable<TEntity>().WhereIF(!string.IsNullOrEmpty(where), where).ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询数据列表
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="whereExpression">whereExpression</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression) //委托里写的过滤条件
        {
            return await _db.Queryable<TEntity>().WhereIF(whereExpression != null, whereExpression).ToListAsync();
        }

        /// <summary>
        /// 功能描述:按照特定列查询数据列表
        /// 作　　者:Blog.Core
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression)
        {
            /*用于从数据库中查询实体并将结果转换为指定的类型。它接受一个参数 expression，这是一个表达式，用于指定查询结果的投影。

假设我们有一个 Person 类，包含 Id、Name 和 Age 三个属性，我们想要查询所有人的 Name 和 Age，可以这样使用 Query 方法：
var result = await Query(p => new { p.Name, p.Age });
AI 生成的代码。仔细查看和使用。 有关常见问题解答的详细信息.
在这个例子中，expression 参数是一个匿名函数 p => new { p.Name, p.Age }，它返回一个匿名对象，包含 Person 类的 Name 和 Age 属性。*/
            return await _db.Queryable<TEntity>().Select(expression).ToListAsync();
        }

        /// <summary>
        /// 功能描述:按照特定列查询数据列表带条件排序
        /// 作　　者:Blog.Core
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="whereExpression">过滤条件</param>
        /// <param name="expression">查询实体条件</param>
        /// <param name="orderByFields">排序条件</param>
        /// <returns></returns>
        public async Task<List<TResult>> Query<TResult>(Expression<Func<TEntity, TResult>> expression, Expression<Func<TEntity, bool>> whereExpression, string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields).WhereIF(whereExpression != null, whereExpression).Select(expression).ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询一个列表
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, string orderByFields)
        {
            return await _db.Queryable<TEntity>().WhereIF(whereExpression != null, whereExpression).OrderByIF(orderByFields != null, orderByFields).ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询一个列表
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="orderByExpression">排序字段</param>
        /// <param name="isAsc">用于指定排序的方向，如果 isAsc 为 true，则按照升序排序；如果 isAsc 为 false，则按照降序排序</param>
        /// <returns></returns>
        public async Task<List<TEntity>> Query(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression, bool isAsc = true)
        {
            //return await Task.Run(() => _db.Queryable<TEntity>().OrderByIF(orderByExpression != null, orderByExpression, isAsc ? OrderByType.Asc : OrderByType.Desc).WhereIF(whereExpression != null, whereExpression).ToList());
            return await _db.Queryable<TEntity>().OrderByIF(orderByExpression != null, orderByExpression, isAsc ? OrderByType.Asc : OrderByType.Desc).WhereIF(whereExpression != null, whereExpression).ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询一个列表
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(string where, string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields).WhereIF(!string.IsNullOrEmpty(where), where).ToListAsync();
        }


        /// <summary>
        /// 功能描述:查询前N条数据
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="top">前N条</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(
            Expression<Func<TEntity, bool>> whereExpression,
            int top,
            string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields).WhereIF(whereExpression != null, whereExpression).Take(top).ToListAsync();
        }

        /// <summary>
        /// 功能描述:查询前N条数据
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="top">前N条</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(
            string where,
            int top,
            string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields).WhereIF(!string.IsNullOrEmpty(where), where).Take(top).ToListAsync();
        }
        #endregion

        #region 原生sql查询
        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">完整的sql语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>泛型集合</returns>
        public async Task<List<TEntity>> QuerySql(string sql, SugarParameter[] parameters = null)
        {
            return await _db.Ado.SqlQueryAsync<TEntity>(sql, parameters);
        }

        /// <summary>
        /// 根据sql语句查询
        /// </summary>
        /// <param name="sql">完整的sql语句</param>
        /// <param name="parameters">参数</param>
        /// <returns>DataTable</returns>
        public async Task<DataTable> QueryTable(string sql, SugarParameter[] parameters = null)
        {
            return await _db.Ado.GetDataTableAsync(sql, parameters);
        }
        #endregion

        #region 分页查询
        /// <summary>
        /// 功能描述:分页查询
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="pageIndex">页码（下标0）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(
            Expression<Func<TEntity, bool>> whereExpression,
            int pageIndex,
            int pageSize,
            string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(whereExpression != null, whereExpression).ToPageListAsync(pageIndex, pageSize);
        }

        /// <summary>
        /// 功能描述:分页查询
        /// 作　　者:Blog.Core
        /// </summary>
        /// <param name="where">条件</param>
        /// <param name="pageIndex">页码（下标0）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns>数据列表</returns>
        public async Task<List<TEntity>> Query(
            string where,
            int pageIndex,
            int pageSize,
            string orderByFields)
        {
            return await _db.Queryable<TEntity>().OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(!string.IsNullOrEmpty(where), where).ToPageListAsync(pageIndex, pageSize);
        }


        /// <summary>
        /// 分页查询[使用版本，其他分页未测试]
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="pageIndex">页码（下标0）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns></returns>
        public async Task<PageModel<TEntity>> QueryPage(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20, string orderByFields = null)
        {
            /*  totalCount 是一个 RefAsync<int> 类型的引用参数，用于存储满足 whereExpression 条件的总记录数。

这个方法的主要作用是分页查询数据库中的实体。它接受四个参数：一个是筛选条件 whereExpression，一个是页码 pageIndex，
一个是每页的记录数 pageSize，还有一个是排序字段 orderByFields。

在查询过程中，ToPageListAsync 方法会计算满足 whereExpression 条件的总记录数，并将结果赋值给 totalCount。
然后，这个方法会根据 pageIndex 和 pageSize 参数，查询对应页码的记录。

最后，这个方法会返回一个 PageModel<TEntity> 对象，其中包含了页码、总记录数、每页的记录数和查询结果。

因此，totalCount 参数的作用就是存储满足筛选条件的总记录数，这对于分页显示数据非常有用。*/
            RefAsync<int> totalCount = 0;
            var list = await _db.Queryable<TEntity>()
                .OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(whereExpression != null, whereExpression)
                .ToPageListAsync(pageIndex, pageSize, totalCount);

            return new PageModel<TEntity>(pageIndex, totalCount, pageSize, list);
        }
        #endregion

        #region 多表查询
        /// <summary> 
        ///查询-多表查询
        /// </summary> 
        /// <typeparam name="T">实体1</typeparam> 
        /// <typeparam name="T2">实体2</typeparam> 
        /// <typeparam name="T3">实体3</typeparam>
        /// <typeparam name="TResult">返回对象</typeparam>
        /// <param name="joinExpression">关联表达式 (join1,join2) => new object[] {JoinType.Left,join1.UserNo==join2.UserNo}</param> 
        /// <param name="selectExpression">返回表达式 (s1, s2) => new { Id =s1.UserNo, Id1 = s2.UserNo}</param>
        /// <param name="whereLambda">查询表达式 (w1, w2) =>w1.UserNo == "")</param> 
        /// <returns>值</returns>
        public async Task<List<TResult>> QueryMuch<T, T2, T3, TResult>(
            /*  这三个参数都是委托类型。
                    *  Func<T, T2, T3, ?> 表示接受三个类型分别为 T，T2，T3 的参数并返回 ? 类型结果的委托。
                    *  通过将委托包裹在 Expression<> 中，可以将其表示为一个可操作的表达式树，而不仅仅是一个简单的委托。
                    *  这样可以在运行时对表达式进行解析、分析和转换，以便在数据库查询等场景中进行更高级的操作。*/
            Expression<Func<T, T2, T3, object[]>> joinExpression,
            Expression<Func<T, T2, T3, TResult>> selectExpression,
            Expression<Func<T, T2, T3, bool>> whereLambda = null) where T : class, new()
        {
            /*  joinExpression：表示多表关联的表达式，是一个 lambda 表达式，接受三个参数 T、T2 和 T3，返回一个对象数组。
             *  这个表达式定义了多个表之间的关联关系。*/

            /*  selectExpression：表示查询结果的映射表达式，也是一个 lambda 表达式，
             *  接受三个参数 T、T2 和 T3，返回一个 TResult 类型的对象。这个表达式定义了将查询结果映射为目标类型的方式。*/

            /*  whereLambda（可选）：表示查询的过滤条件，也是一个 lambda 表达式，
             *  接受三个参数 T、T2 和 T3，返回一个 bool 值。这个表达式用于筛选满足条件的记录。*/
            if (whereLambda == null)
            {
                return await _db.Queryable(joinExpression).Select(selectExpression).ToListAsync();
            }
            /*  比如可能生成的SQL代码大致如下：
             -- RoleModulePermission是按钮跟权限关联表，Role是角色表，Modules是接口API地址信息表
            select r.Name,m.Name,m.LinkUrl from RoleModulePermission as rmp
                left join Modules as m on rmp.ModuleId == m.Id
                left join Role as r on rmp.RoleId == r.Id
                where rmp.IsDeleted == false AND m.IsDeleted == false AND r.IsDeleted == false; */
            return await _db.Queryable(joinExpression).Where(whereLambda).Select(selectExpression).ToListAsync();
        }


        /// <summary>
        /// 两表联合查询-分页
        /// </summary>
        /// <typeparam name="T">实体1</typeparam>
        /// <typeparam name="T2">实体1</typeparam>
        /// <typeparam name="TResult">返回对象</typeparam>
        /// <param name="joinExpression">关联表达式</param>
        /// <param name="selectExpression">返回表达式</param>
        /// <param name="whereExpression">查询表达式</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段</param>
        /// <returns></returns>
        public async Task<PageModel<TResult>> QueryTabsPage<T, T2, TResult>(
            Expression<Func<T, T2, object[]>> joinExpression,
            Expression<Func<T, T2, TResult>> selectExpression,
            Expression<Func<TResult, bool>> whereExpression,
            int pageIndex = 1,
            int pageSize = 20,
            string orderByFields = null)
        {
            RefAsync<int> totalCount = 0;   //总记录数
            var list = await _db.Queryable<T, T2>(joinExpression)
                .Select(selectExpression)
                .OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(whereExpression != null, whereExpression)
                .ToPageListAsync(pageIndex, pageSize, totalCount);
            return new PageModel<TResult>(pageIndex, totalCount, pageSize, list);
        }

        /// <summary>
        /// 两表联合查询-分页-分组
        /// </summary>
        /// <typeparam name="T">实体1</typeparam>
        /// <typeparam name="T2">实体1</typeparam>
        /// <typeparam name="TResult">返回对象</typeparam>
        /// <param name="joinExpression">关联表达式</param>
        /// <param name="selectExpression">返回表达式</param>
        /// <param name="whereExpression">查询表达式</param>
        /// <param name="groupExpression">group表达式</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段</param>
        /// <returns></returns>
        public async Task<PageModel<TResult>> QueryTabsPage<T, T2, TResult>(
            Expression<Func<T, T2, object[]>> joinExpression,
            Expression<Func<T, T2, TResult>> selectExpression,
            Expression<Func<TResult, bool>> whereExpression,
            Expression<Func<T, object>> groupExpression,
            int pageIndex = 1,
            int pageSize = 20,
            string orderByFields = null)
        {
            RefAsync<int> totalCount = 0;   //总记录数
            var list = await _db.Queryable<T, T2>(joinExpression).GroupBy(groupExpression)
                .Select(selectExpression)
                .OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(whereExpression != null, whereExpression)
                .ToPageListAsync(pageIndex, pageSize, totalCount);
            return new PageModel<TResult>(pageIndex, totalCount, pageSize, list);
        }
        #endregion

        //var exp = Expressionable.Create<ProjectToUser>()
        //        .And(s => s.tdIsDelete != true)
        //        .And(p => p.IsDeleted != true)
        //        .And(p => p.pmId != null)
        //        .AndIF(!string.IsNullOrEmpty(model.paramCode1), (s) => s.uID == model.paramCode1.ObjToInt())
        //                .AndIF(!string.IsNullOrEmpty(model.searchText), (s) => (s.groupName != null && s.groupName.Contains(model.searchText))
        //                        || (s.jobName != null && s.jobName.Contains(model.searchText))
        //                        || (s.uRealName != null && s.uRealName.Contains(model.searchText)))
        //                .ToExpression();//拼接表达式
        //var data = await _projectMemberServices.QueryTabsPage<sysUserInfo, ProjectMember, ProjectToUser>(
        //    (s, p) => new object[] { JoinType.Left, s.uID == p.uId },
        //    (s, p) => new ProjectToUser
        //    {
        //        uID = s.uID,
        //        uRealName = s.uRealName,
        //        groupName = s.groupName,
        //        jobName = s.jobName
        //    }, exp, s => new { s.uID, s.uRealName, s.groupName, s.jobName }, model.currentPage, model.pageSize, model.orderField + " " + model.orderType);

        #region Split分表基础接口 （基础CRUD）

        /// <summary>
        /// 分表查询[使用版本，其他分表未测试]
        /// </summary>
        /// <param name="whereExpression">条件表达式</param>
        /// <param name="pageIndex">页码（下标0）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderByFields">排序字段，如name asc,age desc</param>
        /// <returns></returns>
        public async Task<PageModel<TEntity>> QueryPageSplit(Expression<Func<TEntity, bool>> whereExpression, DateTime beginTime, DateTime endTime, int pageIndex = 1, int pageSize = 20, string orderByFields = null)
        {
            /* SplitTable(beginTime, endTime) 是一个方法，用于对数据库表进行分表查询。
             * 它接受两个参数：beginTime 和 endTime，这两个参数用于指定查询的时间范围。*/
            RefAsync<int> totalCount = 0;
            var list = await _db.Queryable<TEntity>().SplitTable(beginTime, endTime)
                .OrderByIF(!string.IsNullOrEmpty(orderByFields), orderByFields)
                .WhereIF(whereExpression != null, whereExpression)
                .ToPageListAsync(pageIndex, pageSize, totalCount);
            var data = new PageModel<TEntity>(pageIndex, totalCount, pageSize, list);
            return data;
        }

        /// <summary>
        /// 写入实体数据
        /// </summary>
        /// <param name="entity">数据实体</param>
        /// <returns></returns>
        public async Task<List<long>> AddSplit(TEntity entity)
        {
            var insert = _db.Insertable(entity).SplitTable();
            //插入并返回雪花ID并且自动赋值ID　
            return await insert.ExecuteReturnSnowflakeIdListAsync();
        }

        /// <summary>
        /// 更新实体数据
        /// </summary>
        /// <param name="entity">数据实体</param>
        /// <returns></returns>
        public async Task<bool> UpdateSplit(TEntity entity, DateTime dateTime)
        {
            //直接根据实体集合更新 （全自动 找表更新）
            //return await _db.Updateable(entity).SplitTable().ExecuteCommandAsync();//,SplitTable不能少

            //精准找单个表
            var tableName = _db.SplitHelper<TEntity>().GetTableName(dateTime); //根据时间获取表名
            return await _db.Updateable(entity).AS(tableName).ExecuteCommandHasChangeAsync();
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public async Task<bool> DeleteSplit(TEntity entity, DateTime dateTime)
        {
            ////直接根据实体集合删除 （全自动 找表插入）,返回受影响数
            //return await _db.Deleteable(entity).SplitTable().ExecuteCommandAsync();//,SplitTable不能少

            //精准找单个表
            var tableName = _db.SplitHelper<TEntity>().GetTableName(dateTime); //根据时间获取表名
            return await _db.Deleteable<TEntity>().AS(tableName).Where(entity).ExecuteCommandHasChangeAsync();
        }

        /// <summary>
        /// 根据ID查找数据
        /// </summary>
        /// <param name="objId"></param>
        /// <returns></returns>
        public async Task<TEntity> QueryByIdSplit(object objId)
        {
            /*  .SplitTable(tabs => tabs)：这是一个分表操作，用于对数据库表进行分表查询。
                在这里，tabs => tabs 是一个表达式，用于指定分表的规则。具体的规则需要根据实际的数据库表结构和数据分布来定义。
                tabs => tabs 是一个 Lambda 表达式，它接受一个参数 tabs，并直接返回这个参数。
                这意味着它不会改变分表的规则，而是直接使用默认的分表规则。*/
            return await _db.Queryable<TEntity>().In(objId).SplitTable(tabs => tabs).SingleAsync();
            //.SingleAsync()：这是一个异步操作，用于返回单个实体。如果查询结果中没有实体或者有多个实体，这个方法会抛出异常。
        }

        #endregion
    }
}