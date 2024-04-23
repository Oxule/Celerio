using Celerio;
using Newtonsoft.Json;

namespace CelerioSamples;

[Service("Pets")]
public static class Pets
{
    public record Animal
    {
        public string Name;
        public enum AnimalType
        {
            Dog,
            Fish,
            Cat,
            Crocodile,
        }
        public AnimalType Type;

        public Animal(string name, AnimalType type)
        {
            Name = name;
            Type = type;
        }
    } 
    public record Pet
    {
        public string Name;
        public Animal Animal;
        public DateTime Taken;

        public Pet(string name, Animal animal, DateTime taken)
        {
            Name = name;
            Animal = animal;
            Taken = taken;
        }
    }

    public record User
    {
        public string Name;
        public List<Pet> Pets;
        public int? Age;
        public bool? Gender;

        public User(string name, List<Pet> pets, int? age = null, bool? gender = null)
        {
            Name = name;
            Pets = pets;
            Age = age;
            Gender = gender;
        }
    }
    
    
    [Response(200, "OK", typeof(User))]
    [Response(401, "Unauthorized")]
    [Route("GET", "/pets/me")]
    public static HttpResponse GetMe()
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(new User("Oxule", new List<Pet>()
        {
            new Pet("Billy", new Animal("Bulldog", Animal.AnimalType.Dog), DateTime.Now),
            new Pet("Dany", new Animal("Cow", Animal.AnimalType.Cat), DateTime.Now),
        }, 15)));
    }
}