using UnityEngine;
using UnityEngine.Events;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Componente para terminales hackeables por el chakra RemoteHack.
    /// Añadir este componente al objeto que debe poder ser hackeado (puerta, terminal, mecanismo).
    /// El objeto debe estar en el layer configurado como terminalLayer en ChakraRemoteHack.
    /// </summary>
    public class HackableTerminal : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] private bool singleUse = true;
        [SerializeField] private UnityEvent onHacked;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer terminalSprite;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color hackedColor = Color.cyan;

        private bool isHacked = false;

        public bool CanBeHacked => !isHacked || !singleUse;
        public bool IsHacked => isHacked;

        public void OnHacked()
        {
            isHacked = true;

            if (terminalSprite != null)
                terminalSprite.color = hackedColor;

            onHacked?.Invoke();

            Debug.Log($"[HackableTerminal] {name} fue hackeada!");
        }

        public void Reset()
        {
            isHacked = false;
            if (terminalSprite != null)
                terminalSprite.color = activeColor;
        }
    }
}
