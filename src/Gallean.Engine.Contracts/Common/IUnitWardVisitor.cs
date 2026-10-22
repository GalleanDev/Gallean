namespace Gallean.Engine.Contracts;

public interface IUnitWardVisitor<in TContext, out TResult>
{
    TResult Visit(UnitAttackClassWard ward, TContext context);
    TResult Visit(UnitAttackSourceWard ward, TContext context);
}
