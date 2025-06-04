local PetDataModule = {}

-- Table of all available pets
PetDataModule.Pets = {
    ["Cat"] = {
        Name = "Cat",
        Icon = "rbxassetid://1234",
        Strength = 5,
        Rarity = "Common"
    },
    ["Dog"] = {
        Name = "Dog",
        Icon = "rbxassetid://5678",
        Strength = 7,
        Rarity = "Uncommon"
    },
    ["Fox"] = {
        Name = "Fox",
        Icon = "rbxassetid://91011",
        Strength = 10,
        Rarity = "Rare"
    },
}

return PetDataModule
