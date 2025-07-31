namespace CodexTestProject;

public interface IAnimal
{
    void Eat();
}

public abstract class AbstractAnimal
{
    public void Eat() { }
}

public class Giraffe : AbstractAnimal, IAnimal
{

}