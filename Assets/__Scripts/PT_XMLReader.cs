using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PT_XMLReader
{
    // Статическая переменная для отображения комментариев, если включено
    static public bool SHOW_COMMENTS = false;

    // Переменная для хранения текста XML
    public string xmlText;
    // Переменная для хранения разобранного XML в виде хэш-таблицы
    public PT_XMLHashtable xml;

    // Метод для парсинга XML-строки и сохранения в xml
    public void Parse(string eS)
    {
        xmlText = eS; // Сохранение текста XML
        xml = new PT_XMLHashtable(); // Инициализация хэш-таблицы
        Parse(eS, xml); // Вызов метода для разбора XML
    }

    // Вспомогательный метод для парсинга XML-строки и добавления данных в хэш-таблицу
    void Parse(string eS, PT_XMLHashtable eH)
    {
        eS = eS.Trim(); // Удаление пробелов в начале и конце строки
        eS = eS.Replace('\t', ' '); // Замена табуляций на пробелы
        while (eS.Length > 0)
        {
            eS = ParseTag(eS, eH); // Парсинг каждого тега
            eS = eS.Trim(); // Удаление пробелов
        }
    }

    // Метод для парсинга отдельного тега
    string ParseTag(string eS, PT_XMLHashtable eH)
    {
        // Поиск индекса начала тега
        int ndx = eS.IndexOf("<");
        int end, end1, end2, end3;

        // Если тег не найден, обрабатываем текстовый узел
        if (ndx == -1)
        {
            end3 = eS.IndexOf(">");
            if (end3 == -1)
            {
                eS = eS.Trim();
                eH.text = eS;
            }
            return ("");
        }

        // Если найден пролог XML (<? ... ?>), то он сохраняется как заголовок
        if (eS[ndx + 1] == '?')
        {
            int ndx2 = eS.IndexOf("?>");
            string header = eS.Substring(ndx, ndx2 - ndx + 2);
            eH.header = header;
            return (eS.Substring(ndx2 + 2));
        }

        // Если найден комментарий (<!-- ... -->), то он сохраняется, если включено SHOW_COMMENTS
        if (eS[ndx + 1] == '!')
        {
            int ndx2 = eS.IndexOf("-->");
            string comment = eS.Substring(ndx, ndx2 - ndx + 3);
            if (SHOW_COMMENTS) Debug.Log("XMl Comment: " + comment);
            return (eS.Substring(ndx2 + 3));
        }

        // Определяем конец тега
        end1 = eS.IndexOf(" ", ndx);
        end2 = eS.IndexOf("/", ndx);
        end3 = eS.IndexOf(">", ndx);
        if (end1 == -1) end1 = int.MaxValue;
        if (end2 == -1) end2 = int.MaxValue;
        if (end3 == -1) end3 = int.MaxValue;

        // Определяем минимальный из найденных индексов конца тега
        end = Mathf.Min(end1, end2, end3);
        string tag = eS.Substring(ndx + 1, end - ndx - 1); // Извлекаем имя тега

        // Если в хэш-таблице еще нет этого тега, создаем новый список
        if (!eH.ContainsKey(tag))
        {
            eH[tag] = new PT_XMLHashList();
        }

        PT_XMLHashList arrL = eH[tag] as PT_XMLHashList; // Получаем список для тега
        PT_XMLHashtable thisHash = new PT_XMLHashtable(); // Создаем новый хэш-объект
        arrL.Add(thisHash); // Добавляем его в список

        // Обработка атрибутов тега
        string atts = "";
        if (end1 < end3)
        {
            try
            {
                atts = eS.Substring(end1, end3 - end1);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("break");
            }
        }

        // Парсинг атрибутов
        string att, val;
        int eqNdx, spNdx;
        while (atts.Length > 0)
        {
            atts = atts.Trim();
            eqNdx = atts.IndexOf("=");
            if (eqNdx == -1) break;
            att = atts.Substring(0, eqNdx);
            spNdx = atts.IndexOf(" ", eqNdx);
            if (spNdx == -1)
            {
                val = atts.Substring(eqNdx + 1);
                if (val[val.Length - 1] == '/')
                {
                    val = val.Substring(0, val.Length - 1);
                }
                atts = "";
            }
            else
            {
                val = atts.Substring(eqNdx + 1, spNdx - eqNdx - 2);
                atts = atts.Substring(spNdx);
            }
            val = val.Trim('\"');
            thisHash.attSet(att, val); // Сохранение атрибута в хэш-объекте
        }

        // Обработка подузлов
        string subs = "";
        string leftoverString = "";

        // Проверка на однострочный тег
        bool singleLine = (end2 == end3 - 1);
        if (!singleLine)
        {
            int close = eS.IndexOf("</" + tag + ">");
            if (close == -1)
            {
                Debug.Log("XMLReader ERROR: XML not well formed. Closing tag </" + tag + "> missing.");
                return ("");
            }
            subs = eS.Substring(end3 + 1, close - end3 - 1);
            leftoverString = eS.Substring(eS.IndexOf(">", close) + 1);
        }
        else
        {
            leftoverString = eS.Substring(end3 + 1);
        }

        subs = subs.Trim();
        if (subs.Length > 0)
        {
            Parse(subs, thisHash); // Рекурсивный вызов для парсинга подузлов
        }

        leftoverString = leftoverString.Trim();
        return (leftoverString);
    }
}

// Класс для хранения списка хэш-объектов
public class PT_XMLHashList
{
    public ArrayList list = new ArrayList(); // Список хэш-объектов

    public PT_XMLHashtable this[int s]
    {
        get
        {
            return (list[s] as PT_XMLHashtable);
        }
        set
        {
            list[s] = value;
        }
    }

    public void Add(PT_XMLHashtable eH)
    {
        list.Add(eH); // Добавление хэш-объекта в список
    }

    public int Count
    {
        get
        {
            return (list.Count); // Возвращает количество элементов в списке
        }
    }

    public int length
    {
        get
        {
            return (list.Count); // Возвращает длину списка
        }
    }
}

// Класс для хранения XML-данных в виде хэш-таблицы
public class PT_XMLHashtable
{
    public List<string> keys = new List<string>(); // Ключи узлов
    public List<PT_XMLHashList> nodesList = new List<PT_XMLHashList>(); // Списки узлов
    public List<string> attKeys = new List<string>(); // Ключи атрибутов
    public List<string> attributesList = new List<string>(); // Списки атрибутов

    // Получение списка узлов по ключу
    public PT_XMLHashList Get(string key)
    {
        int ndx = Index(key);
        if (ndx == -1) return (null);
        return (nodesList[ndx]);
    }

    // Установка значения узла по ключу
    public void Set(string key, PT_XMLHashList val)
    {
        int ndx = Index(key);
        if (ndx != -1)
        {
            nodesList[ndx] = val;
        }
        else
        {
            keys.Add(key);
            nodesList.Add(val);
        }
    }

    // Поиск индекса ключа
    public int Index(string key)
    {
        return (keys.IndexOf(key));
    }

    // Поиск индекса атрибута
    public int AttIndex(string attKey)
    {
        return (attKeys.IndexOf(attKey));
    }

    // Доступ к списку узлов по строковому ключу
    public PT_XMLHashList this[string s]
    {
        get
        {
            return (Get(s));
        }
        set
        {
            Set(s, value);
        }
    }

    // Получение значения атрибута по ключу
    public string att(string attKey)
    {
        int ndx = AttIndex(attKey);
        if (ndx == -1) return ("");
        return (attributesList[ndx]);
    }

    // Установка значения атрибута
    public void attSet(string attKey, string val)
    {
        int ndx = AttIndex(attKey);
        if (ndx == -1)
        {
            attKeys.Add(attKey);
            attributesList.Add(val);
        }
        else
        {
            attributesList[ndx] = val;
        }
    }

    // Свойство для работы с текстовым содержимым узла
    public string text
    {
        get
        {
            int ndx = AttIndex("@");
            if (ndx == -1) return ("");
            return (attributesList[ndx]);
        }
        set
        {
            int ndx = AttIndex("@");
            if (ndx == -1)
            {
                attKeys.Add("@");
                attributesList.Add(value);
            }
            else
            {
                attributesList[ndx] = value;
            }
        }
    }

    // Свойство для работы с заголовком XML
    public string header
    {
        get
        {
            int ndx = AttIndex("@XML_Header");
            if (ndx == -1) return ("");
            return (attributesList[ndx]);
        }
        set
        {
            int ndx = AttIndex("@XML_Header");
            if (ndx == -1)
            {
                attKeys.Add("@XML_Header");
                attributesList.Add(value);
            }
            else
            {
                attributesList[ndx] = value;
            }
        }
    }

    // Свойство для получения ключей узлов
    public string nodes
    {
        get
        {
            string s = "";
            foreach (string key in keys)
            {
                s += key + "   ";
            }
            return (s);
        }
    }

    // Свойство для получения ключей атрибутов
    public string attributes
    {
        get
        {
            string s = "";
            foreach (string attKey in attKeys)
            {
                s += attKey + "   ";
            }
            return (s);
        }
    }

    // Проверка наличия ключа узла
    public bool ContainsKey(string key)
    {
        return (Index(key) != -1);
    }

    // Проверка наличия ключа атрибута
    public bool ContainsAtt(string attKey)
    {
        return (AttIndex(attKey) != -1);
    }

    // Проверка наличия узла по ключу
    public bool HasKey(string key)
    {
        return (Index(key) != -1);
    }

    // Проверка наличия атрибута по ключу
    public bool HasAtt(string attKey)
    {
        return (AttIndex(attKey) != -1);
    }
}
