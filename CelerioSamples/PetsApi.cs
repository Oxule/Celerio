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
        public int? Age;
        public bool? Gender;

        public User(string name, int? age = null, bool? gender = null)
        {
            Name = name;
            Age = age;
            Gender = gender;
        }
    }

    public static List<Pet> StaticPets = new List<Pet>()
    {
        new ("Billy", new Animal("Bulldog", Animal.AnimalType.Dog), DateTime.Now),
        new ("Dany", new Animal("Cow", Animal.AnimalType.Cat), DateTime.MinValue),
        new ("Joshua", new Animal("Goldfish", Animal.AnimalType.Fish), DateTime.MaxValue),
    };
    
    [Response(200, "OK", typeof(User))]
    [Response(401, "Unauthorized")]
    [Route("GET", "/pets/me")]
    public static HttpResponse GetMe()
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(new User("Oxule", 15)));
    }
    
    [Response(200, "OK", typeof(User))]
    [Response(401, "Unauthorized")]
    [Route("PUT", "/pets/me")]
    public static HttpResponse PutMe(User body)
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(body));
    }
    
    [Response(200, "OK", typeof(User))]
    [Response(401, "Unauthorized")]
    [Route("DELETE", "/pets/me")]
    public static HttpResponse DeleteMe()
    {
        return HttpResponse.Ok("Deleted!");
    }
    
    [Response(200, "OK", typeof(List<Pet>))]
    [Response(401, "Unauthorized")]
    [Route("GET", "/pets")]
    public static HttpResponse GetPets()
    {
        return HttpResponse.Ok(JsonConvert.SerializeObject(StaticPets));
    }
    
    [Response(200, "OK", typeof(List<Pet>))]
    [Response(401, "Unauthorized")]
    [Route("POST", "/pet")]
    public static HttpResponse PostPet(Pet body)
    {
        var p = StaticPets;
        p.Add(body);
        return HttpResponse.Ok(JsonConvert.SerializeObject(p));
    }
    
    [Response(200, "OK", typeof(Pet))]
    [Response(404, "Not Found")]
    [Response(401, "Unauthorized")]
    [Route("GET", "/pet/{id}")]
    public static HttpResponse GetPet(int id)
    {
        if(id < 0 || id >= StaticPets.Count)
            return HttpResponse.NotFound();
        return HttpResponse.Ok(JsonConvert.SerializeObject(StaticPets[id]));
    }
    
    [Response(200, "OK", typeof(List<Pet>))]
    [Response(404, "Not Found")]
    [Response(401, "Unauthorized")]
    [Route("DELETE", "/pet/{id}")]
    public static HttpResponse DeletePet(int id)
    {
        if(id < 0 || id >= StaticPets.Count)
            return HttpResponse.NotFound();
        var p = StaticPets;
        p.RemoveAt(id);
        return HttpResponse.Ok(JsonConvert.SerializeObject(p));
    }
}