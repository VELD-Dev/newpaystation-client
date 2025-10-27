using NewPayStation.Client;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// Dans les projets de type SDK comme celui-là, plusieurs attributs d'assembly définis
// historiquement dans ce fichier sont maintenant automatiquement ajoutés pendant
// la génération et renseignés avec des valeurs définies dans les propriétés du projet.
// Pour plus d'informations sur les attributs à inclure et sur la personnalisation
// de ce processus, consultez : https://aka.ms/assembly-info-properties


// La définition de ComVisible sur False rend les types dans cet assembly invisibles
// aux composants COM. Si vous devez accéder à un type dans cet assembly à partir
// de COM, définissez l'attribut ComVisible sur True pour ce type.

[assembly: ComVisible(false)]

// Le GUID suivant concerne l'ID de typelib si ce projet est exposé à COM.

[assembly: Guid("493bd32b-913e-4c7e-9c23-2ea2201cec5e")]
[assembly: AssemblyVersion(AssemblyInfo.VERSION)]
[assembly: AssemblyFileVersion(AssemblyInfo.VERSION)]
[assembly: AssemblyCompany(AssemblyInfo.AUTHOR)]
[assembly: AssemblyProduct(AssemblyInfo.APP_NAME)]
[assembly: AssemblyCopyright(AssemblyInfo.LICENSE + " License - Copyright © 2024 " + AssemblyInfo.AUTHOR)]
[assembly: AssemblyInformationalVersion(AssemblyInfo.DISPLAY_VERSION)]
[assembly: CLSCompliant(true)]
[assembly: AssemblyMetadata("RepositoryUrl", AssemblyInfo.GIT_URL)]

namespace NewPayStation.Client;

public static class AssemblyInfo
{
    public const string VERSION = "1.1.0.0";
    public const string DISPLAY_VERSION = "v1.1.0";
    public const string APP_NAME = "NewPayStation Client";
    public const string AUTHOR = "VELD-Dev";
    public const string GIT_URL = "https://github.com/VELD-Dev/newpaystation-client";
    public const string LICENSE = "GPLv3";
    public const string LICENSE_URL = "https://github.com/VELD-Dev/newpaystation-client/blob/main/LICENSE.txt";
    public static string AssemblyHash
    {
        get
        {
            var hash = SHA256.Create();
            using (var stream = File.OpenRead(Assembly.GetExecutingAssembly().Location))
            {
                hash.ComputeHash(stream);
            }
            return Convert.ToBase64String(hash.Hash ?? []);
        }
    }
}  
