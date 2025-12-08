(********************************************************
Author: Sergio Alex
Operating System: MS Windows XP, 7, 8, 10
License: GNU General Public License (GPL)
Category: Multi-User Dungeons (MUD)
*********************************************************)

//Módulo libre de bibliotecas externas
unit cadenas;

interface

uses Objetos,Demonios;

const
  NOMBRE_FONT='Times New Roman';

  MC_TipoDeArma:array[0..8] of string[12]=(
    'Cortante','Punzante','Contundente','Veneno','Fuego','Hielo','Rayo','Magia','Munición');

  MC_NombresComerciantes:array[0..MAX_TIPOS_COMERCIO] of string[15]=(
    'Minero','Armero','Armero','Armero','Armero','Maestro Armero',
    'Maestro Armero','Sastre','Herrero','Carpintero','Hostelero','Joyero',
    'Diamantista','Herbalista','Alquimista','Mercader','Hechicero','Clérigo','Marinero',
    'Maestro Armero','Gran Carpintero','Maestro Sastre','Gemólogo','4','5','6','7','8','9','X','Y','Z');

  MC_Pericias:array[0..15] of string[15]=(
    'Alquimia','Escribir magia','Herbalismo','Joyería',
    'Herrería','Carpintería','Sastrería','Holgazanería',
    'Minería','Elocuencia','Regeneración','Ambidextría',
    'Apuñalar','Ocultarse','Ira Berserker','Zoomorfismo');

  MC_Nombre_Categoria:array[0..7] of string[13]=(
    'Guerrero','Clérigo','Mago','Pícaro','Montaraz','Paladín','Bardo','Guerrero Mago');

  MC_Nombre_Categoria2:array[0..7] of string[15]=(
    'Campeón','Maestre Clérigo','Archimago','Maestre Asesino','Guardabosques','Maestre Paladín','Maestre Bardo','Campeón Arcano');

  MC_Genero:array[0..1] of string[5]=('Varón','Mujer');

  Nombre_Ataque:array[0..MaxNombresAtaques] of string[15]=
  ('ácido','aguijón','alabarda','aliento','arcabuz','arco y flecha','ballesta',
   'cola','cuernos','daga','embestida','espada','fuego','garra',
   'golpe','hacha','hechizo','hielo','lanza','mandoble','maza',
   'dardo venenoso','mordida','patada','picotazo','rayo','tenazas',
   'flecha venenosa','aliento frio','dedo de muerte');

  MaxNombresRecursos=47;

  Nombre_Recurso:array[0..MaxNombresRecursos] of string[23]=(
    'SIN RECURSO',
    'Leña',
    'Madera',
    'Madera Mágica',
    'Mineral de Hierro',
    'Mineral de Arcanita',
    'Mineral de Plata',
    'Mineral de Oro',
    'Capullos de mariposas',
    'Aceite de salamandra',
    'Muérdago seco',
    'Pétalos de orquídea',
    'Nido de araña infernal',
    'Baba de medusa',
    'Huesos de dragón',
    'Lágrimas de unicornio',
    'Mina de Amatistas',
    'Mina de Topacios',
    'Mina de Aguamarinas',
    'Mina de Zafíros',
    'Mina de Esmeraldas',
    'Mina de Rubíes',
    'Mina de Diamantes',
    'Mina de Sol oscuro',
    '*Vegetal0',
    '*Vegetal1',
    '*Vegetal2',
    '*Vegetal3',
    '*Vegetales',
    'Mina de Gemas',
    'Baul Mágico',
    '*Hiladora',
    'Fundición',
    'Telar',
    'Yunque',
    'Tina de curtido',
    'Taller de carpintería',
    'Mesa de sastrería',
    'Castillo',
    'Laboratorio de alquimia',
    'Estudio de hechicería',
    'Taller de joyería',
    '*No usado 1',
    '*No usado 2',
    '*No usado 3',
    '*Bendición',
    '*Conjuración',
    '*Observatorio');

function DescribirObjeto(objeto:TArtefacto;const CapIdentificacion:TcapacidadIdentificacion):string;

implementation

function DescribirObjeto(objeto:TArtefacto;const CapIdentificacion:TcapacidadIdentificacion):string;
var BonoVisible,ModificadorVisible:integer;
  tipoDanno:TTipoArma;
  function intastrPorcentual(valor:integer):string;
  begin//Para mostrar armas y armaduras encantadas
    str(valor,result);
    if valor>0 then result:='+'+result;
    result:=result+'%';
  end;
begin
  with infObj[objeto.id] do
  begin
    tipoDanno:=TipoArma;
    if ((Objeto.modificador and MskIdentificado)<>0) or (CapIdentificacion=ciVerRealmente) then
    begin
      BonoVisible:=calcularBono(Objeto,tipoDanno);
      ModificadorVisible:=calcularModificadorAtaDef(Objeto);
    end
    else
    begin
      BonoVisible:=0;
      ModificadorVisible:=infObj[objeto.id].modificadorADC;
    end;
    case objeto.id of
      //armas conjuradas, municiones, no tienen bonos
      8..15,48..55:result:='Ataque: '+intastrPorcentual(infObj[objeto.id].modificadorADC)+
        ', Daño '+MC_TipoDeArma[ord(TipoArma)]+': '+NivelDanno(danno1B,danno1P)+' PM, '+NivelDanno(danno2B,danno2P)+' G';
      //armas
      16..47:result:='Ataque: '+intastrPorcentual(ModificadorVisible)+
        ', Daño '+MC_TipoDeArma[integer(tipoDanno)]+': '+NivelDanno(danno1B+BonoVisible,danno1P)+' PM, '+NivelDanno(danno2B+BonoVisible,danno2P)+' G';
      //armaduras
      56..103,248..253:result:='Evasión: '+intastrPorcentual(ModificadorVisible)+
        '  Punz: '+ArmaduraPorcentual(danno1B)+
        '  Cort: '+ArmaduraPorcentual(danno1P)+
        '  Cont: '+ArmaduraPorcentual(danno2B)+
        '  Mágia: '+ArmaduraPorcentual(danno2P+BonoVisible);
    else
      result:='';
    end;
  end;
end;


end.
