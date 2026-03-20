namespace NABHI.Character
{
    /// <summary>
    /// Interfaz para entidades que pueden recibir daño
    /// Implementada por PlayerHealth, enemigos, objetos destructibles, etc.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Aplica daño a la entidad
        /// </summary>
        /// <param name="damage">Cantidad de daño a aplicar</param>
        void TakeDamage(float damage);

        /// <summary>
        /// Verifica si la entidad está viva
        /// </summary>
        /// <returns>True si está viva, False si está muerta</returns>
        bool IsAlive();

        /// <summary>
        /// Cura a la entidad (opcional)
        /// </summary>
        /// <param name="amount">Cantidad de salud a restaurar</param>
        void Heal(float amount);
    }
}
