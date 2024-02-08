Aplicación de Consola para interpretar líneas de código en lenguaje HULK, soporta declaraciones
de funciones (que se pueden usar hasta que se cierre la app de consola, refrescándose cuando se
reinicia) (incluyendo recursivas), chequeo de tipos estático (con inferencia de tipos global), expresiones let-in, if-elif-else
y expresiones matemáticas, lógicas y de concatenación de strings.

.Net 7.0 sdk necesario para correr el proyecto

Simplemente correr (dotnet run) la app de consola en Program.cs para ejecutarlo

Escribir comandos después del carácter '>', terminando cada línea con ';'

Tipos: number, string, boolean

Literales booleanos: true, false

Funciones Pre-construidas: log(x) (base 10), sin(x), cos(x), sqrt(x), print(x) (simplemente imprime x)

Variables Pre-construidas: E (euler), PI

En declaraciones de funciones, si el contexto donde son usados los parámetros y el
tipo de retorno no es suficiente para inferir sus tipos, especificar tipos de la forma:

function f(n: number) => n == n;

function f(b) : boolean => b; // especificando el tipo de retorno de f

tipos: 'number', 'boolean', 'string'.

ejemplo de input:

> function f(n) => if (n > 2) f(n - 1) + f(n - 2) else 1;
> f(5)
120
